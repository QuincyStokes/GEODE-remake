using UnityEngine;

public class PlayerAnimationSFX : MonoBehaviour
{
    public void PlayWalkSFX()
    {
        //Do some logic based on what we're standing on.
        AudioManager.Instance.PlayLocal(SoundId.Grass_Walk, transform.position);
    }
}
