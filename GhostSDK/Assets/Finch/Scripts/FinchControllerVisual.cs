// Copyright 2018 Finch Technologies Ltd. All rights reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using UnityEngine;

namespace Finch
{
    /// <summary>
    /// Visualises controller's buttons elements.
    /// </summary>
    [Serializable]
    public class Buttons
    {
        /// <summary>
        /// Type of controller element.
        /// </summary>
        public FinchControllerElement Type;

        /// <summary>
        /// Pressed button visualisation model.
        /// </summary>
        public MeshRenderer Button;

        private readonly Color pressed = new Color(0.671f, 0.671f, 0.671f);
        private readonly Color unpressed = Color.black;
        /// <summary>
        /// Update pressing state of buttons.
        /// </summary>
        /// <param name="isPressing"></param>
        public void UpdateState(bool isPressing)
        {
            Button.material.color = isPressing ? pressed : unpressed;
            Button.material.SetColor("_EmissionColor", isPressing ? pressed : unpressed);
        }
    }

    [Serializable]
    /// <summary>
    /// Used to visualize battery power.
    /// </summary>
    public class BatteryLevel
    {
        /// <summary>
        /// Sprite, visualizing a certain level of charge.
        /// </summary>
        public Sprite BatteryMaterial;

        /// <summary>
        /// The level of charge in percent.
        /// </summary>
        [Range(0, 100)]
        public int MinimumBatteryBorder;
    }

    /// <summary>
    /// Visualises controller's buttons, stick or touchpad and battery level.
    /// </summary>
    public class FinchControllerVisual : MonoBehaviour
    {
        [Header("Chirality")]
        /// <summary>
        /// Controller chirality.
        /// </summary>
        public FinchChirality Chirality;

        [Header("Model")]
        /// <summary>
        /// Object to visualise controller state.
        /// </summary>
        public GameObject Model;

        [Header("State")]
        /// <summary>
        /// Represents information about should we hide controllers in calibration module.
        /// </summary>
        public bool HideInCalibration = true;

        [Header("Buttons")]
        /// <summary>
        /// List of visualisable buttons.
        /// </summary>
        public Buttons[] Buttons = new Buttons[0];

        [Header("Battery")]
        /// <summary>
        /// Battery level renderer.
        /// </summary>
        public SpriteRenderer BatteryObject;

        /// <summary>
        /// Array of different charge level materials.
        /// </summary>
        public BatteryLevel[] BatteryLevels = new BatteryLevel[4];

        [Header("Touch element")]
        /// <summary>
        /// Touch point model element transform.
        /// </summary>
        public Transform TouchPoint;

        /// <summary>
        /// Stick model element transform.
        /// </summary>
        public Transform Joystick;

        /// <summary>
        /// Visualisation of touchpad modification.
        /// </summary>
        public GameObject TouchpadModel;

        /// <summary>
        /// Visualisation of joustick modification.
        /// </summary>
        public GameObject JoystickModel;

        private FinchController controller;
        private float batteryLevel;
        private float touchPointPower;

        private const float epsilon = 0.05f;
        private const float chargeLevelEpsilon = 1.5f;
        private const float touchPointDepth = 0.001f;
        private const float maxAngleRotation = 18.0f;
        private const float touchPadRadius = 0.0175f;
        private const float touchPointRadius = 0.0056f;
        private const float scaleTimer = 0.15f;

        private void LateUpdate()
        {
            controller = FinchController.GetController(Chirality);

            ButtonUpdate();
            BatteryUpdate();
            UpdateJoystick();
            UpdateTouchpad();
            StateUpdate();
        }

        private void StateUpdate()
        {
            bool hideCauseCalibration = FinchCalibration.IsCalbrating && HideInCalibration;
            bool hideCauseDisconnect = !controller.IsConnected;
            Model.SetActive(!hideCauseCalibration && !hideCauseDisconnect);
        }

        private void ButtonUpdate()
        {
            bool activeTouchPad = controller.GetPress(FinchControllerElement.Touch) && controller.TouchAxes.SqrMagnitude() > epsilon;

            foreach (var b in Buttons)
            {
                b.UpdateState(controller.GetPress(b.Type) && (b.Type != FinchControllerElement.Touch || activeTouchPad));
            }
        }

        private void BatteryUpdate()
        {
            if (BatteryObject == null)
            {
                return;
            }

            bool isBatteryActive = (FinchInput.IsConnected(controller.Node)) && BatteryLevels.Length > 0;
            if (BatteryObject.gameObject.activeSelf != isBatteryActive)
            {
                BatteryObject.gameObject.SetActive(isBatteryActive);
            }

            float currentBatteryLevel = Mathf.Clamp(FinchInput.GetBatteryCharge(controller.Node), 0f, 99.9f);
            if (isBatteryActive && Math.Abs(currentBatteryLevel - batteryLevel) > chargeLevelEpsilon)
            {
                Sprite batterySprite = null;
                float maxBorder = 0;

                batteryLevel = currentBatteryLevel;

                foreach (BatteryLevel i in BatteryLevels)
                {
                    if (currentBatteryLevel > i.MinimumBatteryBorder && maxBorder <= i.MinimumBatteryBorder)
                    {
                        maxBorder = i.MinimumBatteryBorder;
                        batterySprite = i.BatteryMaterial;
                    }
                }

                BatteryObject.sprite = batterySprite;
            }
        }

        private void UpdateJoystick()
        {
            if (JoystickModel != null)
            {
                JoystickModel.SetActive(controller.IsJoystickAvailable);
            }

            if (Joystick != null)
            {
                Joystick.transform.localEulerAngles = new Vector3(controller.TouchAxes.y, 0, -controller.TouchAxes.x) * maxAngleRotation;
            }
        }

        private void UpdateTouchpad()
        {
            if (TouchpadModel != null)
            {
                TouchpadModel.SetActive(controller.IsTouchpadAvailable);
            }

            Vector3 size = new Vector3(touchPointRadius, touchPointDepth, touchPointRadius);
            float speed = Time.deltaTime / Mathf.Max(epsilon, scaleTimer) * (controller.GetPress(FinchControllerElement.Touch) ? 1 : -1);

            touchPointPower = controller.IsTouchpadAvailable ? Mathf.Clamp01(touchPointPower + speed) : 0;

            if (TouchPoint != null)
            {
                TouchPoint.localScale = controller.GetPress(FinchControllerElement.ThumbButton) ? Vector3.zero : size * touchPointPower;

                if (controller.IsTouching)
                {
                    TouchPoint.localPosition = new Vector3(controller.TouchAxes.x, TouchPoint.localPosition.y, controller.TouchAxes.y) * touchPadRadius;
                }
            }
        }
    }
}
