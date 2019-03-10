using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GhostCamera : MonoBehaviour
{

    public struct Device
    {
        public float width;
        public float height;
        public float bottom;
        public Vector3 offset;
    };

    private Device currDevice = new Device{
        #if UNITY_EDITOR
            width = 0.13f,
            height = 0.07f,
        #else
            width = Mathf.Max((Screen.width / Screen.dpi) * 0.0254f, (Screen.height / Screen.dpi) * 0.0254f),
            height = Mathf.Min((Screen.width / Screen.dpi) * 0.0254f, (Screen.height / Screen.dpi) * 0.0254f),
        #endif
        bottom = 0.0045f,
        offset = new Vector3(0.058f, -0.055f, -0.0080f)
    };

    public Camera cameraRight;
    public Camera cameraLeft;
    public Transform offsetGO;

    private float ipd = 0.064f;
    private float near = 0.06544f + 0.008f;
    private float focalLength = 0.069f;
    private float viewHeight = 0.051f;
    private float viewWidth = 0.059f;
    private float viewBottom = 0.007f;
    private Vector3 headOffset = new Vector3(0f, -0.030f, -0.07046f - 0.008f);

    // Start is called before the first frame update
    void Start()
    {
        InitDevice();
        InitViewports();
        offsetGO.localPosition = headOffset + currDevice.offset;
    }

    void InitDevice()
    {
        string androidDevice = SystemInfo.deviceModel;
        #if UNITY_IOS
            var iosDevice = UnityEngine.iOS.Device.generation;

            if(iosDevice == UnityEngine.iOS.DeviceGeneration.iPhone6S || iosDevice == UnityEngine.iOS.DeviceGeneration.iPhone7 || iosDevice == UnityEngine.iOS.DeviceGeneration.iPhone8){
                currDevice.width = 0.104f;
                currDevice.height = 0.058f;
                currDevice.bottom = 0.0045f;
                currDevice.offset = new Vector3(0.058f, -0.055f, -0.0080f);
            }
            else if(iosDevice == UnityEngine.iOS.DeviceGeneration.iPhone6SPlus || iosDevice == UnityEngine.iOS.DeviceGeneration.iPhone7Plus || iosDevice == UnityEngine.iOS.DeviceGeneration.iPhone8Plus){
                currDevice.width = 0.122f;
                currDevice.height = 0.068f;
                currDevice.bottom = 0.0045f;
                currDevice.offset = new Vector3(0.0569f, -0.063f, -0.0086f);
            }
            else if(iosDevice == UnityEngine.iOS.DeviceGeneration.iPhoneX){
                currDevice.width = 0.135f;
                currDevice.height = 0.062f;
                currDevice.bottom = 0.0045f;
                currDevice.offset = new Vector3(0.060f, -0.055f, -0.009f);
            }
        #elif UNITY_ANDROID
            if(androidDevice == "Google Pixel"){
                currDevice.width = 0.122f;
                currDevice.height = 0.068f;
                currDevice.bottom = 0.0045f;
                currDevice.offset = new Vector3(0.071f, -0.056f, -0.008f);
            }
            if(androidDevice == "Google Pixel 2"){
                currDevice.width = 0.12f;
                currDevice.height = 0.060f;
                currDevice.bottom = 0f;
                currDevice.offset = new Vector3(0.071f, -0.056f, -0.008f);
            }
            if(androidDevice == "Google Pixel XL" || androidDevice == "Google Pixel 2 XL"){
                currDevice.width = 0.12f;
                currDevice.height = 0.060f;
                currDevice.bottom = 0.0045f;
                currDevice.offset = new Vector3(0.071f, -0.056f, -0.008f);
            }
            if(androidDevice == "samsung SM-G950F"){
                currDevice.width = 0.132f;
                currDevice.height = 0.067f;
                currDevice.bottom = 0.0015f;
                currDevice.offset = new Vector3(0.071f, -0.056f, -0.008f);
            }
        #endif
    }

    void InitViewports()
    {
        cameraLeft.rect = GetViewportRect(false);
        cameraRight.rect = GetViewportRect(true);

        cameraLeft.transform.localPosition = new Vector3(-ipd / 2f, 0f, 0f);
        cameraRight.transform.localPosition = new Vector3(ipd / 2f, 0f, 0f);

        cameraLeft.nearClipPlane = cameraRight.nearClipPlane = near;
        float far = cameraLeft.farClipPlane = cameraRight.farClipPlane = 100f;

        Rect viewportLeftInMeter = GetViewportRectInMeter(false);
        Rect viewportRightInMeter = GetViewportRectInMeter(true);

        Matrix4x4 leftEyeProjectionMatrix = Matrix4x4.zero;
        Matrix4x4 rightEyeProjectionMatrix = Matrix4x4.zero;

        leftEyeProjectionMatrix[0, 0] = 2.0f * near / viewportLeftInMeter.width;
        leftEyeProjectionMatrix[1, 1] = 2.0f * near / viewportLeftInMeter.height;
        leftEyeProjectionMatrix[0, 2] = (ipd - viewportLeftInMeter.width) / viewportLeftInMeter.width;
        leftEyeProjectionMatrix[2, 2] = -(far + near) / (far - near);
        leftEyeProjectionMatrix[2, 3] = -(2.0f * far * near) / (far - near);
        leftEyeProjectionMatrix[3, 2] = -1.0f;
        rightEyeProjectionMatrix = leftEyeProjectionMatrix;
        rightEyeProjectionMatrix[0, 2] = (viewportRightInMeter.width - ipd) / viewportRightInMeter.width;

        cameraLeft.projectionMatrix = leftEyeProjectionMatrix;
        cameraRight.projectionMatrix = rightEyeProjectionMatrix;
    }

    public Rect GetViewportRect()
    {
        Rect result = new Rect(0f, 0f, 1f, 1f);

        float b = (viewBottom - currDevice.bottom) / currDevice.height;
        float x = 0f;
        float y = 0f;
        float w = 1f;
        float h = 1f;

        float aw = (viewWidth * 2f) / currDevice.width;
        float ah = viewHeight / currDevice.height;

        w = Mathf.Clamp01(aw);
        h = Mathf.Clamp01(ah + b) - b;
        x = 0.5f - (w / 2f);
        y = b;
        result = new Rect(x, y, w, h);

        return result;
    }

    public Rect GetViewportRectInMeter(bool isRight)
    {
        Rect rect = GetViewportRect(isRight);
        return Rect.MinMaxRect(rect.xMin * currDevice.width, rect.yMin * currDevice.height,
                               rect.xMax * currDevice.width, rect.yMax * currDevice.height);
    }

    public Rect GetViewportRect(bool isRight)
    {
        Rect viewportRect = GetViewportRect();
        if (isRight)
        {
            return new Rect(
                0.5f,
                viewportRect.y,
                viewportRect.width / 2f,
                viewportRect.height);
        }
        else{
            return new Rect(
                0.5f - (viewportRect.width / 2f),
                viewportRect.y,
                viewportRect.width / 2f,
                viewportRect.height);
        }
    }

    void Update()
    {
        
    }
}
