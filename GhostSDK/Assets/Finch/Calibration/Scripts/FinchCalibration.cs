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

using UnityEngine;
using System;

namespace Finch
{
    /// <summary>
    /// Describes type of calibration.
    /// </summary>
    public enum CalibrationType
    {
        FullCalibration,
        FastCalibration
    }

    /// <summary>
    /// Calibration settings.
    /// </summary>
    [Serializable]
    public class FinchCalibrationSettings
    {
        /// <summary>
        /// Type of calibration.
        /// </summary>
        public CalibrationType Calibration = CalibrationType.FastCalibration;

        public bool AvailableMomentalCalibration = true;

        /// <summary>
        /// Load calibration module on start.
        /// </summary>
        public bool CalibrateOnStart = true;

        /// <summary>
        /// Load calibration module on resume.
        /// </summary>
        public bool CalibrateOnResume = true;

        /// <summary>
        /// Load calibration module on controller disconnect.
        /// </summary>
        public bool CalibrateOnDisconnect = true;

        /// <summary>
        /// Call scanner each time you calibrate.
        /// </summary>
        public bool Rescanning = false;

        /// <summary>
        /// Time pressing home button to call calibration module.
        /// </summary>
        public float TimePressingToCallCalibration = 0.3f;

        /// <summary>
        /// Haptic time after calibration.
        /// </summary>
        public ushort HapticTime = 120;
    }

    /// <summary>
    /// Manages calibration module.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class FinchCalibration : MonoBehaviour
    {
        /// <summary>
        /// Calibration settings.
        /// </summary>
        public static FinchCalibrationSettings Settings = new FinchCalibrationSettings();

        public static bool IsCalbrated { get; private set; }

        /// <summary>
        /// Is calibration module active.
        /// </summary>
        public static bool IsCalbrating { get; private set; }

        /// <summary>
        /// Action that happens when calibration started.
        /// </summary>
        public static Action OnCalibrationStart;

        /// <summary>
        /// Action that happens when calibration ended.
        /// </summary>
        public static Action OnCalibrationEnd;

        [Header("Calibration settings")]
        /// <summary>
        /// Certain calibration options.
        /// </summary>
        public FinchCalibrationSettings CalibrationOptions = new FinchCalibrationSettings();

        [Header("Sound")]
        /// <summary>
        /// Calibration start sound.
        /// </summary>
        public AudioClip StartCalibration;

        /// <summary>
        /// Calibration step pass sound.
        /// </summary>
        public AudioClip Succes;

        [Header("Steps")]
        /// <summary>
        /// Calibration module steps.
        /// </summary>
        public TutorialStep[] FullCalibration = new TutorialStep[0];

        /// <summary>
        /// Calibration module steps.
        /// </summary>
        public TutorialStep[] FastCalibration = new TutorialStep[0];

        /// <summary>
        ///  Incorrect set tutorial step.
        /// </summary>
        public TutorialStep IncorrectSet;

        private static FinchCalibration instance;
        private static CalibrationType calibrationType = CalibrationType.FullCalibration;

        private AudioSource audioSource;
        private bool leftReadyCalibrate;
        private bool rightReadyCalibrate;
        private bool onPaused;

        private bool availableMomentalCalibration
        {
            get
            {
                bool leftCapacityCorrect = !FinchController.Left.IsConnected || FinchCore.GetCapacitySensor(FinchNodeType.LeftHand) == FinchChirality.Left;
                bool rightCapacityCorrect = !FinchController.Right.IsConnected || FinchCore.GetCapacitySensor(FinchNodeType.RightHand) == FinchChirality.Right;
                bool angleCorrect = NodeAngleChecker.IsCorrectAngle || FinchCore.NodesState.GetUpperArmCount() == 0;
                return leftCapacityCorrect && rightCapacityCorrect && angleCorrect;
            }
        }

        /// <summary>
        /// Calibration module call.
        /// </summary>
        public static void Calibrate()
        {
            Calibrate(Settings.Calibration);
        }

        /// <summary>
        /// Load calibration module.
        /// </summary>
        public static void Calibrate(CalibrationType type)
        {
            calibrationType = type;
            LoadStep(0);
        }

        private void Awake()
        {
            IsCalbrated = false;
            audioSource = GetComponent<AudioSource>();
            Settings = CalibrationOptions;
            FinchCore.OnDisconnected += OnDisconnectNode;
            PlayableSet.Init();
        }

        private void Start()
        {
            instance = this;
            TurnOffSteps();
            if (Settings.CalibrateOnStart)
            {
                Calibrate(CalibrationType.FullCalibration);
            }
        }

        private void Update()
        {
            NodeAngleChecker.Update();

            if (onPaused)
            {
                onPaused = false;
                if (Settings.CalibrateOnResume)
                {
                    Calibrate(CalibrationType.FullCalibration);
                }
            }

            UpdatePressing();
            TryCalibrate();
        }

        private void OnApplicationPause(bool pause)
        {
            onPaused |= pause;
            IsCalbrated = false;
        }

        private void OnDisconnectNode(FinchNodeType node)
        {
            if (!PlayableSet.AllPlayableNodesConnected && CalibrationOptions.CalibrateOnDisconnect)
            {
                IsCalbrating = true;
                IsCalbrated = false;
                TurnOffSteps();
                IncorrectSet.Init(0);
            }
        }

        private void TurnOffSteps()
        {
            foreach (TutorialStep i in FastCalibration)
            {
                i.gameObject.SetActive(false);
            }

            foreach (TutorialStep i in FullCalibration)
            {
                i.gameObject.SetActive(false);
            }

            IncorrectSet.gameObject.SetActive(false);
        }

        private void UpdatePressing()
        {
            leftReadyCalibrate &= !IsCalbrating;
            rightReadyCalibrate &= !IsCalbrating;
            leftReadyCalibrate |= !FinchController.Left.HomeButton;
            rightReadyCalibrate |= !FinchController.Right.HomeButton;
        }

        private void TryCalibrate()
        {
            if (IsCalbrating)
            {
                return;
            }

            bool leftReady = !FinchController.Left.IsConnected || leftReadyCalibrate && FinchController.Left.GetPressTime(FinchControllerElement.HomeButton) > Settings.TimePressingToCallCalibration;
            bool rightReady = !FinchController.Right.IsConnected || rightReadyCalibrate && FinchController.Right.GetPressTime(FinchControllerElement.HomeButton) > Settings.TimePressingToCallCalibration;

            if (Settings.AvailableMomentalCalibration)
            {
                if (FinchCore.Settings.ControllerType == FinchControllerType.Dash)
                {
                    if (leftReady)
                    {
                        FastCalibrate(FinchController.Left);
                        leftReadyCalibrate = false;
                    }

                    if (rightReady)
                    {
                        FastCalibrate(FinchController.Right);
                        rightReadyCalibrate = false;
                    }
                }
                else if (leftReady && rightReady)
                {
                    if (availableMomentalCalibration)
                    {
                        leftReadyCalibrate = false;
                        rightReadyCalibrate = false;
                        FinchController.Left.HapticPulse(Settings.HapticTime);
                        FinchController.Right.HapticPulse(Settings.HapticTime);
                        FinchCore.Calibration(FinchChirality.Both);
                    }
                    else
                    {
                        leftReadyCalibrate = false;
                        rightReadyCalibrate = false;

                        ResetCalibration();
                        Calibrate(Settings.Calibration);
                    }
                }
            }
            else if (FinchCore.NodesState.GetControllersCount() > 0 && leftReady && rightReady)
            {
                leftReadyCalibrate = false;
                rightReadyCalibrate = false;

                ResetCalibration();
                Calibrate(Settings.Calibration);
            }
        }

        /// <summary>
        /// Loads the calibration step by its Id.
        /// </summary>
        /// <param name="stepId">Tutorial step Id.</param>
        public static void LoadStep(int stepId)
        {
            if (instance != null)
            {
                instance.TurnOffSteps();
                instance.LoadCalibrationStep(stepId);
            }
        }

        private void LoadCalibrationStep(int stepId)
        {
            if (stepId <= 0)
            {
                audioSource.Stop();
                IsCalbrated = false;
                IsCalbrating = true;

                if (OnCalibrationStart != null)
                {
                    OnCalibrationStart();
                }

                if (StartCalibration != null)
                {
                    Play(StartCalibration);
                }
            }
            else if (Succes != null)
            {
                Play(Succes);
            }

            TutorialStep[] steps = calibrationType == CalibrationType.FullCalibration ? FullCalibration : FastCalibration;

            if (stepId >= 0 && stepId < steps.Length)
            {
                steps[stepId].Init(stepId);
            }

            if (stepId >= steps.Length)
            {
                IsCalbrated = true;
                IsCalbrating = false;

                if (OnCalibrationEnd != null)
                {
                    OnCalibrationEnd();
                }
            }
        }

        /// <summary>
        /// Plays an audio clip.
        /// </summary>
        /// <param name="clip">Audio clip used for calibration.</param>
        public void Play(AudioClip clip)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }

        private void FastCalibrate(FinchController controller)
        {
            controller.HapticPulse(Settings.HapticTime);
            controller.Calibrate();
        }

        private void ResetCalibration()
        {
            bool wasLeftReverted = FinchCore.Interop.FinchIsUpperArmReverted(FinchCore.Interop.FinchChirality.Left) == 1;
            bool wasRightReverted = FinchCore.Interop.FinchIsUpperArmReverted(FinchCore.Interop.FinchChirality.Right) == 1;

            FinchCore.ResetCalibration(FinchChirality.Both);

            if (wasLeftReverted)
            {
                FinchCore.Interop.FinchRevertUpperArm(FinchCore.Interop.FinchChirality.Left);
            }

            if (wasRightReverted)
            {
                FinchCore.Interop.FinchRevertUpperArm(FinchCore.Interop.FinchChirality.Right);
            }

            FinchCore.Update();
            NodeAngleChecker.Update();
        }
    }
}
