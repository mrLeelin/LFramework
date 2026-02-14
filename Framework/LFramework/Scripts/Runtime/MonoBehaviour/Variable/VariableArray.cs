
using System.Collections.Generic;
using UnityEngine;


namespace LFramework.Runtime
{
    [System.Serializable]
    public class VariableArray
    {
        [SerializeField] private List<Variable> variables;


        public IReadOnlyCollection<Variable> Variables => variables.AsReadOnly();

        public Variable this[int index] => variables[index];

        public void Set(string key,object value)
        {
            if (string.IsNullOrEmpty(key))
            {
                return;
            }

            if (variables == null || variables.Count <= 0)
            {
                return ;
            }

            var variable = variables.Find(x => x.Name == key);
            if (variable == null)
            {
                return;
            }
            variable.SetValue(value);
        }
        public object Get(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            if (variables == null || variables.Count <= 0)
            {
                return null;
            }

            var variable = variables.Find(x => x.Name == key);
            return variable?.GetValue();
        }

        public T Get<T>(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return default;
            }

            if (variables == null || variables.Count <= 0)
            {
                return default;
            }

            var variable = variables.Find(x => x.Name == key);
            return variable == null ? default : variable.GetValue<T>();
        }

        public T Get<T>(string key, T defaultValue)
        {
            if (string.IsNullOrEmpty(key))
            {
                return defaultValue;
            }

            if (variables == null || variables.Count <= 0)
            {
                return defaultValue;
            }

            var variable = variables.Find(x => x.Name == key);
            return variable == null ? defaultValue : variable.GetValue<T>();
        }

        public static implicit operator List<Variable>(VariableArray array)
        {
            return array.variables;
        }

        public static implicit operator VariableArray(List<Variable> variables)
        {
            return new VariableArray() { variables = variables };
        }
    }
}