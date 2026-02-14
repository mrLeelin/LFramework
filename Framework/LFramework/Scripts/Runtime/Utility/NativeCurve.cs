using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace LFramework.Runtime
{
    public struct NativeCurve : IDisposable
    {
        private NativeArray<float> _samples;
        private int _resolution;

        public void Initialize(AnimationCurve curve, int resolution, Allocator allocator)
        {
            this._resolution = resolution;
            _samples = new NativeArray<float>(resolution, allocator);

            for (int i = 0; i < resolution; i++)
            {
                float t = i / (float)(resolution - 1);
                _samples[i] = curve.Evaluate(t);
            }
        }

        public float Evaluate(float t)
        {
            t = math.saturate(t);
            float scaled = t * (_resolution - 1);
            int index = (int)scaled;
            int next = math.min(index + 1, _resolution - 1);
            float frac = scaled - index;
            return math.lerp(_samples[index], _samples[next], frac);
        }

        public void Dispose()
        {
            if (_samples.IsCreated)
                _samples.Dispose();
        }
    }
}