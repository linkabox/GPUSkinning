using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GPUSkinningExecuteOncePerFrame
{
    private int frameCount = -1;

    public bool CanBeExecute()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            return frameCount != Time.frameCount;
        }
        else
        {
            return true;
        }
#else
		return frameCount != Time.frameCount;
#endif
    }

    public void MarkAsExecuted()
    {
#if UNITY_EDITOR
        if (Application.isPlaying)
        {
            frameCount = Time.frameCount;
        }
#else
		frameCount = Time.frameCount;
#endif
    }
}
