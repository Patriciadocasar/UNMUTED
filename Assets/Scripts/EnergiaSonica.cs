using UnityEngine;

public class EnergiaSonica : MonoBehaviour
{
    public float maxima = 100f;
    public float Actual { get; private set; }
    public bool EstaLlena => Actual >= maxima;

    public void Sumar(float cantidad) =>
        Actual = Mathf.Clamp(Actual + cantidad, 0f, maxima);

    public void GastarTodo() => Actual = 0f;
}