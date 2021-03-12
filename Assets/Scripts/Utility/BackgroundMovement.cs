using System;
using System.Collections;
using System.Collections.Generic;
using GliderBoy.Controllers;
using GliderBoy.Utility;
using UnityEngine;

public class BackgroundMovement : MonoBehaviour
{
    
    [Serializable]
    public class Background
    {
        
        #region Fields

        public Transform background;
        public float speed = 5.0f;
        public float removalPoint = -15.329f;
        public float replacePoint = 15.329f;

        public Transform[] children;
        
        #endregion

        #region Public Functions
        
        public void Initialise()
        {
            if (background == null) return;
            
            children = new Transform[background.childCount];
            for (var i = 0; i < children.Length; i++) children[i] = background.GetChild(i);
        }
        
        public void Move()
        {
            foreach (var child in children)
                child.Translate(Vector3.left * speed * Time.deltaTime);
        }

        public void ChildCheck()
        {
            foreach (var child in children)
                if (child.localPosition.x < removalPoint)
                    child.localPosition = Vector3.zero.WithX(replacePoint);
        }
        
        #endregion
        
    }

    public Background[] backgrounds;
    private bool _paused;

    public void PauseMovement(bool pause) => _paused = pause;

    
    
    #region MonoBehaviour
    
    private void Start()
    {
        GameController.Instance.OnPauseAction += PauseMovement;

        _paused = true;
        
        if(backgrounds.Length == 0) Destroy(this);
        
        foreach (var background in backgrounds)
            background.Initialise();
    }

    private void Update()
    {
        if(_paused) return;
        
        foreach (var background in backgrounds)
        {
            background.Move();
            background.ChildCheck();
        }
    }

    #endregion
    
}
