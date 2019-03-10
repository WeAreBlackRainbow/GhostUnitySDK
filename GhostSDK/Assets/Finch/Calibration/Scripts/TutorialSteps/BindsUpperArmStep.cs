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

namespace Finch
{
    /// <summary>
    /// Responsible for assigning Finch Upper Arm nodes chirality.
    /// </summary>
    public class BindsUpperArmStep : TutorialStep
    {
        private class TrembleOptions
        {
            public int TrembleCount;

            public bool DirectionPass
            {
                get
                {
                    bool biggerZero = Mathf.Max(sideAboveZero, sideBelowZero) > 0;
                    bool leftSuccess = (sideAboveZero / Mathf.Max(epsilon, sideBelowZero) > orientationRatio);
                    bool rightSuccess = (sideBelowZero / Mathf.Max(epsilon, sideAboveZero) > orientationRatio);
                    return biggerZero && (leftSuccess || rightSuccess);
                }
            }

            public bool RevertOrientation { get { return sideAboveZero / Mathf.Max(epsilon, sideBelowZero) > orientationRatio; } }

            public float sideAboveZero;
            public float sideBelowZero;

            private FinchNodeType node;
            private bool directionUp;
            private float lastAcceleration;

            public TrembleOptions(FinchNodeType node)
            {
                this.node = node;
            }

            public void UpdateState()
            {
                TrembleAcceleration();
                ArmDirection();
            }

            private void TrembleAcceleration()
            {
                Vector3 upperArmAccel = FinchCore.GetNodeLinearAcceleration(node);
                float acceleration = (upperArmAccel - Vector3.up * g).sqrMagnitude;

                if (Mathf.Abs(acceleration - lastAcceleration) > accelerationBorder)
                {
                    if ((acceleration - lastAcceleration > 0) != directionUp)
                    {
                        directionUp = !directionUp;
                        TrembleCount++;
                    }

                    lastAcceleration = acceleration;
                }
            }

            private void ArmDirection()
            {
                Vector3 origin = (node == FinchNodeType.RightUpperArm) ? Vector3.right : Vector3.left;
                float y = (FinchCore.GetNodeRotation(node, false) * origin).y;

                if (Mathf.Abs(y) < angleBorder)
                {
                    return;
                }

                if (y > 0)
                {
                    sideAboveZero += y;
                }
                else
                {
                    sideBelowZero -= y;
                }
            }

            public void ResetValue()
            {
                directionUp = false;
                lastAcceleration = 0;
                TrembleCount = 0;
                sideAboveZero = 0;
                sideBelowZero = 0;
            }
        }

        [Header("Tutorials")]
        /// <summary>
        /// An object that visualizes the part of the tutorial that is responsible for shaking with the right hand.
        /// </summary>
        public GameObject TutorialShake;

        /// <summary>
        /// An object that visualizes the part of the tutorial that is responsible for lowering the hand down.
        /// </summary>
        public GameObject TutorialArmsDown;

        /// <summary>
        /// An object that visualizes the part of the tutorial that is responsible for prevent shaking left arm.
        /// </summary>
        public GameObject WarningLeftArmShaking;

        [Header("Timer options")]
        /// <summary>
        ///  Time to go through the shaking hand stage.
        /// </summary>
        public float TimeShakeTutorial = 2.5f;

        /// <summary>
        /// Time for the passage of the stage of lowering hands.
        /// </summary>
        public float TimeArmsDownTutorial = 1.5f;

        /// <summary>
        /// Error display time.
        /// </summary>
        public float TimeErrorShow = 2f;

        private const int minShakingCount = 4;
        private const float epsilon = 0.01f;
        private const float accelerationRatio = 5f;
        private const float accelerationBorder = 30.0f;
        private const float angleBorder = 0.3f;
        private const float g = 9.8f;
        private const float orientationRatio = 2f;

        private TrembleOptions leftArm = new TrembleOptions(FinchNodeType.LeftUpperArm);
        private TrembleOptions rightArm = new TrembleOptions(FinchNodeType.RightUpperArm);

        private float timeGetResult;

        private bool tremblePass;
        private bool leftArmShaking;

        public override void Init(int id)
        {
            base.Init(id);

            if (FinchCore.Interop.FinchIsUpperArmReverted(FinchCore.Interop.FinchChirality.Left) == 1)
            {
                FinchCore.Interop.FinchRevertUpperArm(FinchCore.Interop.FinchChirality.Left);
            }

            if (FinchCore.Interop.FinchIsUpperArmReverted(FinchCore.Interop.FinchChirality.Right) == 1)
            {
                FinchCore.Interop.FinchRevertUpperArm(FinchCore.Interop.FinchChirality.Right);
            }

            FinchCore.Interop.FinchResetCalibration(FinchCore.Interop.FinchChirality.Left);
            FinchCore.Interop.FinchResetCalibration(FinchCore.Interop.FinchChirality.Right);

            bool rightDifferentNodes = FinchController.Right.IsConnected && FinchInput.IsConnected(FinchNodeType.LeftUpperArm);
            bool leftDifferentNodes = FinchController.Left.IsConnected && FinchInput.IsConnected(FinchNodeType.RightUpperArm);

            if (FinchCore.NodesState.GetUpperArmCount() == 1 && (rightDifferentNodes || leftDifferentNodes))
            {
                FinchCore.SwapNodes(FinchNodeType.LeftUpperArm, FinchNodeType.RightUpperArm);
            }

            tremblePass = FinchCore.NodesState.GetUpperArmCount() < 2;
            leftArmShaking = false;

            ResetValue();
            TryPassStepWithoutRedefine();
        }

        private void Update()
        {
            TryPassStepWithoutRedefine();
            TryBindChiralityManually();
            TryPassStepWithRedefine();

            UpdateSprite();
            UpdatePosition();
        }

        private void TryBindChiralityManually()
        {
            bool pressLeft = FinchInput.GetPressDown(FinchNodeType.LeftUpperArm, FinchControllerElement.HomeButton);
            bool pressRight = FinchInput.GetPressDown(FinchNodeType.RightUpperArm, FinchControllerElement.HomeButton);

            if (pressLeft || pressRight)
            {
                if (pressLeft)
                {
                    FinchCore.SwapNodes(FinchNodeType.LeftUpperArm, FinchNodeType.RightUpperArm);
                }

                if (!tremblePass)
                {
                    ResetValue();
                    tremblePass = true;
                }
            }
        }

        private void TryPassStepWithRedefine()
        {
            if (Time.time <= timeGetResult)
            {
                leftArm.UpdateState();
                rightArm.UpdateState();
            }

            bool leftToRightRatio = leftArm.TrembleCount / Mathf.Max(rightArm.TrembleCount, epsilon) > accelerationRatio;
            bool rightToLeftRatio = rightArm.TrembleCount / Mathf.Max(leftArm.TrembleCount, epsilon) > accelerationRatio;

            bool tremble = Mathf.Max(leftArm.TrembleCount, rightArm.TrembleCount) > minShakingCount && (leftToRightRatio || rightToLeftRatio);
            bool leftDirectionCorrect = !FinchInput.IsConnected(FinchNodeType.LeftUpperArm) || leftArm.DirectionPass;
            bool rightDirectionCorrect = !FinchInput.IsConnected(FinchNodeType.RightUpperArm) || rightArm.DirectionPass;
            bool swapNodes = FinchCore.NodesState.GetUpperArmCount() == 2 && leftToRightRatio;
            if (Time.time > timeGetResult || tremble && !tremblePass)
            {
                if ((tremblePass || tremble) && leftDirectionCorrect && rightDirectionCorrect)
                {
                    BindsOrientation(swapNodes);
                    BindsChirality(swapNodes);
                    BindUpperArms();
                    NextStep();
                    return;
                }

                if (tremble && !tremblePass)
                {
                    BindsChirality(leftToRightRatio);
                    tremblePass = true;
                    ResetValue();
                    return;
                }

                leftArmShaking = Mathf.Max(leftArm.TrembleCount, rightArm.TrembleCount) > minShakingCount;

                ResetValue();
            }
        }

        private void UpdateSprite()
        {
            TutorialArmsDown.SetActive(tremblePass);
            TutorialShake.SetActive(!tremblePass && !leftArmShaking);
            WarningLeftArmShaking.SetActive(!tremblePass && leftArmShaking);
        }

        private void BindsChirality(bool swapNodes)
        {
            if (swapNodes)
            {
                FinchCore.SwapNodes(FinchNodeType.LeftUpperArm, FinchNodeType.RightUpperArm);
            }
        }

        private void BindsOrientation(bool swapNodes)
        {
            if (leftArm.RevertOrientation && swapNodes || rightArm.RevertOrientation && !swapNodes)
            {
                FinchCore.Interop.FinchRevertUpperArm(FinchCore.Interop.FinchChirality.Right);
            }

            if (rightArm.RevertOrientation && swapNodes || leftArm.RevertOrientation && !swapNodes)
            {
                FinchCore.Interop.FinchRevertUpperArm(FinchCore.Interop.FinchChirality.Left);
            }
        }

        private void TryPassStepWithoutRedefine()
        {
            if (FinchCore.NodesState.GetUpperArmCount() == 0 || FinchCore.NodesState.GetUpperArmCount() < FinchCore.NodesState.GetControllersCount())
            {
                BindUpperArms();
                NextStep();
            }
        }

        private void BindUpperArms()
        {
            FinchCore.BindsUpperArms();
            int controllerCount = FinchCore.NodesState.GetControllersCount();
            int upperArmsCount = FinchCore.NodesState.GetUpperArmCount();
            PlayableSet.RememberNodes(controllerCount, controllerCount > upperArmsCount ? 0 : controllerCount);
        }

        private void ResetValue()
        {
            timeGetResult = Time.time + (FinchCore.NodesState.GetUpperArmCount() > 1 ? TimeShakeTutorial : TimeArmsDownTutorial);
            leftArm.ResetValue();
            rightArm.ResetValue();
        }
    }
}
