using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using UnityEngine;

class GIRer
{
    public delegate void RenderCallback();

    public const string GIRerPath = "BepInEx\\plugins\\GIRer.dll";

    [DllImport(GIRerPath, EntryPoint = "AddRenderCallback")]
    public static extern void AddRenderCallback(RenderCallback callback);

    [DllImport(GIRerPath, EntryPoint = "GIRerLoad")]
    public static extern void Enable();

    [DllImport(GIRerPath, EntryPoint = "DrawFrame2D")]
    public static extern void DrawFrame2D(float x, float y, float xSclae, float ySclae, float r, float g, float b, float a);

    [DllImport(GIRerPath, EntryPoint = "DrawRect2D")]
    public static extern void DrawRect2D(float x, float y, float xSclae, float ySclae, float r, float g, float b, float a);
}
