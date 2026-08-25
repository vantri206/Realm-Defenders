using System;
using System.Globalization;
using TMPro;
using UnityEngine;

[Serializable]
public class UIValueTextBinding
{
    [SerializeField] private TMP_Text text;
    [SerializeField] private string emptyValue = "-";

    public TMP_Text Text => text;
    public string EmptyValue => emptyValue;

    public void Show()
    {
        SetVisible(true);
    }

    public void Hide()
    {
        SetVisible(false);
    }

    public void SetVisible(bool isVisible)
    {
        if (text != null)
        {
            text.gameObject.SetActive(isVisible);
        }
    }

    public void Refresh()
    {
        SetText(emptyValue);
    }

    public void SetTextColor(Color color)
    {
        if (text == null)
        {
            Debug.LogError("[UIValueTextBinding] TMP_Text reference is required before setting text color.");
            return;
        }

        text.color = color;
    }

    public void SetText(string value)
    {
        if (text == null)
        {
            Debug.LogError("[UIValueTextBinding] TMP_Text reference is required before setting text.");
            return;
        }

        text.text = string.IsNullOrEmpty(value) ? emptyValue : value;
    }

    public void SetInt(int value)
    {
        SetText(value.ToString(CultureInfo.InvariantCulture));
    }

    public void SetInt(float value)
    {
        SetInt((int)Math.Round(value, MidpointRounding.AwayFromZero));
    }

    public void SetFloat(float value, string format = "0.#")
    {
        SetText(FormatNumber(value, format));
    }

    public void SetSeconds(float value)
    {
        SetText($"{FormatNumber(value)}s");
    }

    private static string FormatNumber(float value, string format = "0.#")
    {
        return value.ToString(format, CultureInfo.InvariantCulture);
    }
}
