﻿// Copyright 2018 Finch Technologies Ltd. All rights reserved.
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
    /// Calibration step for assigning chirality when using a one 6 Dof set.
    /// </summary>
    public class BindOne6Dof : TutorialStep
    {
        /// <summary>
        /// Spite renderer object.
        /// </summary>
        public SpriteRenderer Sprite;

        /// <summary>
        /// Set to rigth hand image.
        /// </summary>
        public Sprite SetToRightHand;

        /// <summary>
        /// Set to left hand image.
        /// </summary>
        public Sprite SetToLeftHand;

        public override void Init(int id)
        {
            base.Init(id);
            TryPassStep();
        }

        void Update()
        {
            UpdatePosition();
            TryPassStep();
            Sprite.sprite = GetUpperArmChirality() == FinchChirality.Left ? SetToLeftHand : SetToRightHand;
        }

        private void TryPassStep()
        {
            bool correctSet = FinchCore.NodesState.GetControllersCount() == 1 && FinchCore.NodesState.GetUpperArmCount() == 1;

            bool leftCapacityCorrect = !FinchController.Left.IsConnected || FinchCore.GetCapacitySensor(FinchNodeType.LeftHand) == GetUpperArmChirality();
            bool rightCapacityCorrect = !FinchController.Right.IsConnected || FinchCore.GetCapacitySensor(FinchNodeType.RightHand) == GetUpperArmChirality();

            if (!correctSet || leftCapacityCorrect && rightCapacityCorrect)
            {
                NextStep();
            }
        }

        private FinchChirality GetUpperArmChirality()
        {
            return FinchInput.IsConnected(FinchNodeType.LeftHand) ? FinchChirality.Left : FinchChirality.Right;
        }
    }
}
