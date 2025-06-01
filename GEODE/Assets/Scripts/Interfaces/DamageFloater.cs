using System;
using System.Collections;
using TMPro;
using Unity.Mathematics;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class DamageFloater : MonoBehaviour
{
    [SerializeField] private TMP_Text damageTextUI;
    [SerializeField] private float floatSpeed;
    [SerializeField] private float floatTime;
    private Color textColor;
    
    public void Initialize(float damage)
    {
        damageTextUI.text = Mathf.RoundToInt(damage).ToString();
        textColor = damageTextUI.color;
    }

    private void Start()
    {
        StartCoroutine(FloatAndFade());   
    }



    private IEnumerator FloatAndFade()
    {
        //TODO here can add color changes, scale changes, etc based on damage/time/whateva
        float elapsed = 0f;
        while (elapsed <= floatTime)
        {
            elapsed += Time.deltaTime;
            transform.position += new Vector3(0, floatSpeed) * Time.deltaTime;
            textColor.a = 1 - elapsed/floatTime;
            damageTextUI.color = textColor;
            yield return null;
        }
        Destroy(gameObject);
    }

}
