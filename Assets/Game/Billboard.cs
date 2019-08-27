using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class Billboard : MonoBehaviour {

    Camera m_Camera;

    void Start() {
        m_Camera = Camera.main;
    }
    
    void Update()
    {
        if (m_Camera == null)
            return;
        transform.LookAt(transform.position + m_Camera.transform.forward, m_Camera.transform.up);
    }
}
