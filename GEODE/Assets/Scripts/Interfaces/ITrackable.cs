using System;
using UnityEngine;

public interface ITrackable
{
    public event Action<StatTrackType, string> OnSingleTrack;
    public event Action<StatTrackType, string, int> OnMultiTrack;
}
