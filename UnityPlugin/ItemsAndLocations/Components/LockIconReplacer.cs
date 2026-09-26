using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Archskipelagill.ItemsAndLocations.Components;
public class LockIconReplacer:MonoBehaviour {

    ////// Initializer/Fields/Properties //////
    
    public SpriteRenderer Renderer { get; private set; }
    public bool IsArchiLocked { get; set; } = false;

    private Sprite _originalSprite;


    ////// Public API //////

    public void UpdateIcon() {
        if(Renderer == null) {
            if(TryGetComponent<SpriteRenderer>(out var renderer)) {
                Renderer = renderer;
                _originalSprite = Renderer.sprite;
            }
        }
        if(Renderer != null)
            Renderer.sprite = IsArchiLocked ? AbilityUnlockItemizer.Instance.CustomLockSprite : _originalSprite;
    }
}
