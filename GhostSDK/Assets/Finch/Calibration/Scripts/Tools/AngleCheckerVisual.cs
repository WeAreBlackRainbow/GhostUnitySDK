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
using Finch;

[RequireComponent(typeof(SpriteRenderer))]
public class AngleCheckerVisual : MonoBehaviour
{
    public Color Success = new Color(0, 0.5f, 0);
    public Color Fail = new Color(0.5f, 0, 0);

    private SpriteRenderer sprite;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    private void OnEnable()
    {
        UpdateColor();
    }

    private void Update()
    {
        UpdateColor();
    }

    private void UpdateColor()
    {
        if (FinchCore.NodesState.GetUpperArmCount() > 0)
        {
            sprite.color = NodeAngleChecker.IsCorrectAngle ? Success : Fail;
        }
        else
        {
            sprite.color = new Color(0, 0, 0, 0);
        }
    }
}
