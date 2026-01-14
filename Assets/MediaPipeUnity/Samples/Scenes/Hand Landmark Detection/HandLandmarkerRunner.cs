// Copyright (c) 2023 homuler
//
// Use of this source code is governed by an MIT-style
// license that can be found in the LICENSE file or at
// https://opensource.org/licenses/MIT.

using System;
using System.Collections;
using Mediapipe.Tasks.Vision.HandLandmarker;
using UnityEngine;
using UnityEngine.Rendering;

namespace Mediapipe.Unity.Sample.HandLandmarkDetection
{
  public class HandLandmarkerRunner : VisionTaskApiRunner<HandLandmarker>
  {
    [SerializeField] private HandLandmarkerResultAnnotationController _handLandmarkerResultAnnotationController;

    // ★追加：Annotation（点・線）を動かしたくないなら false
    [SerializeField] private bool _enableAnnotations = true;

    // ★追加：メインスレッドで結果を受け取るイベント
    public event Action<HandLandmarkerResult> OnHandLandmarkerResult;

    private Experimental.TextureFramePool _textureFramePool;

    public readonly HandLandmarkDetectionConfig config = new HandLandmarkDetectionConfig();

    // ★追加：LIVE_STREAMコールバック→Updateへ渡すためのバッファ
    private readonly object _resultLock = new object();
    private bool _hasPendingResult = false;
    private HandLandmarkerResult _pendingResult;   // コールバック側でCloneToする先
    private HandLandmarkerResult _mainThreadResult; // Update側で受け取ってイベント発火する用

    private void Update()
    {
      // ★コールバックで受けた結果を、メインスレッドでイベント発火
      bool fire = false;

      lock (_resultLock)
      {
        if (_hasPendingResult)
        {
          // pending -> mainThread に移し替え
          _pendingResult.CloneTo(ref _mainThreadResult);
          _hasPendingResult = false;
          fire = true;
        }
      }

      if (fire)
      {
        OnHandLandmarkerResult?.Invoke(_mainThreadResult);
      }
    }

    public override void Stop()
    {
      base.Stop();
      _textureFramePool?.Dispose();
      _textureFramePool = null;
    }

    protected override IEnumerator Run()
    {
      Debug.Log($"Delegate = {config.Delegate}");
      Debug.Log($"Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"Running Mode = {config.RunningMode}");
      Debug.Log($"NumHands = {config.NumHands}");
      Debug.Log($"MinHandDetectionConfidence = {config.MinHandDetectionConfidence}");
      Debug.Log($"MinHandPresenceConfidence = {config.MinHandPresenceConfidence}");
      Debug.Log($"MinTrackingConfidence = {config.MinTrackingConfidence}");

      yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

      // ★LIVE_STREAMでやる（InspectorでもOKだが、事故防止で強制）
      config.RunningMode = Tasks.Vision.Core.RunningMode.LIVE_STREAM;

      // ★LIVE_STREAMは「必ず」コールバック必須
      var options = config.GetHandLandmarkerOptions(OnHandLandmarkDetectionOutput);

      taskApi = HandLandmarker.CreateFromOptions(options, GpuManager.GpuResources);

      var imageSource = ImageSourceProvider.ImageSource;
      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Debug.LogError("Failed to start ImageSource, exiting...");
        yield break;
      }

      // Use RGBA32 as the input format.
      // TODO: When using GpuBuffer, MediaPipe assumes that the input format is BGRA, so maybe the following code needs to be fixed.
      _textureFramePool = new Experimental.TextureFramePool(imageSource.textureWidth, imageSource.textureHeight, TextureFormat.RGBA32, 10);

      // NOTE: The screen will be resized later, keeping the aspect ratio.
      screen.Initialize(imageSource);

      // ★AnnotationのON/OFF
      if (_enableAnnotations)
      {
        SetupAnnotationController(_handLandmarkerResultAnnotationController, imageSource);
      }
      else
      {
        if (_handLandmarkerResultAnnotationController != null)
        {
          _handLandmarkerResultAnnotationController.gameObject.SetActive(false);
        }
      }

      var transformationOptions = imageSource.GetTransformationOptions();
      var flipHorizontally = transformationOptions.flipHorizontally;
      var flipVertically = transformationOptions.flipVertically;
      var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(rotationDegrees: (int)transformationOptions.rotationAngle);

      AsyncGPUReadbackRequest req = default;
      var waitUntilReqDone = new WaitUntil(() => req.done);
      var waitForEndOfFrame = new WaitForEndOfFrame();

      // NOTE: we can share the GL context of the render thread with MediaPipe (for now, only on Android)
      var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
      using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

      while (true)
      {
        if (isPaused)
        {
          yield return new WaitWhile(() => isPaused);
        }

        if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
        {
          yield return new WaitForEndOfFrame();
          continue;
        }

        // Build the input Image
        Image image;
        switch (config.ImageReadMode)
        {
          case ImageReadMode.GPU:
            if (!canUseGpuImage)
            {
              throw new System.Exception("ImageReadMode.GPU is not supported");
            }
            textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildGPUImage(glContext);
            // TODO: Currently we wait here for one frame to make sure the texture is fully copied to the TextureFrame before sending it to MediaPipe.
            // This usually works but is not guaranteed. Find a proper way to do this. See: https://github.com/homuler/MediaPipeUnityPlugin/pull/1311
            yield return waitForEndOfFrame;
            break;

          case ImageReadMode.CPU:
            yield return waitForEndOfFrame;
            textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;

          case ImageReadMode.CPUAsync:
          default:
            req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            yield return waitUntilReqDone;

            if (req.hasError)
            {
              Debug.LogWarning($"Failed to read texture from the image source");
              continue;
            }
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
        }

        // ★LIVE_STREAM：非同期に投げる（結果は OnHandLandmarkDetectionOutput に返る）
        taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
      }
    }

    private void OnHandLandmarkDetectionOutput(HandLandmarkerResult result, Image image, long timestamp)
    {
      // ★Annotation：サンプル同様 DrawLater は「後で描く」なのでコールバックから呼んでOK
      if (_enableAnnotations && _handLandmarkerResultAnnotationController != null)
      {
        _handLandmarkerResultAnnotationController.DrawLater(result);
      }

      // ★結果を保存（コールバックは別スレッドの可能性があるので lock）
      lock (_resultLock)
      {
        result.CloneTo(ref _pendingResult);
        _hasPendingResult = true;
      }
    }
  }
}
