using Octothorpe.UGity.Editor.Util;
using System.Reflection;
using System.Runtime.CompilerServices;
using System;

using UnityEditor;

using UnityEngine;

namespace Octothorpe.UGity.Editor.UI
{
    public abstract partial class GitEditorWindow
    {
        public abstract class Prompt : Modal
        {
            private const string CONFIRM_DEFAULT = "Confirm";
            
            protected static readonly Vector2 promptSize = new Vector2(400f, 100f);
            
            protected sealed override string Title { get; set; }
            protected sealed override bool HasHeader { get; } = false;

            protected bool Cancelled { get; set; } = true;
            protected string ConfirmButton { get; set; }

            public static T Create<T>(string title, out bool cancelled, params string[] labels)
            {
                return Create<T>(title, CONFIRM_DEFAULT, out cancelled, labels);
            }

            public static T Create<T>(string title, string confirmButton, out bool cancelled, params string[] labels)
            {
                if(typeof(ITuple).IsAssignableFrom(typeof(T)))
                    return MultiPrompt.GetResult<T>(title, confirmButton, out cancelled, labels);
                else
                    return SinglePrompt.GetResult<T>(title, confirmButton, out cancelled, labels[0]);
            }

            protected static object DrawField(object value, Type type, string label)
            {
                if(type == typeof(string))
                    return EditorGUILayout.TextField(label, (string) value);
                else if(type == typeof(bool))
                    return EditorGUILayout.ToggleLeft(label, (bool) value);
                else if(type == typeof(int))
                    return EditorGUILayout.IntField(label, (int) value);

                throw new NotSupportedException($"Type {type.Name} is not supported by prompts.");
            }
            
            protected abstract void DrawFields();

            protected sealed override void OnDraw()
            {
                this.DrawFields();

                GUILayout.FlexibleSpace();

                using(new GUILayout.HorizontalScope())
                {
                    if(GUILayout.Button("Cancel"))
                    {
                        this.OnCancel();
                        Close();
                        return;
                    }

                    GUILayout.FlexibleSpace();
                    
                    if(GUILayout.Button("Enter"))
                    {
                        this.OnConfirm();
                        Cancelled = false;
                        Close();
                        return;
                    }
                }
            }

            protected virtual void OnConfirm() { }

            protected virtual void OnCancel() { }
        }

        protected T CreatePrompt<T>(string title, string confirmButton, out bool cancelled, params string[] labels) => Prompt.Create<T>(title, confirmButton, out cancelled, labels);

        protected T CreatePrompt<T>(string title, out bool cancelled, params string[] labels) => Prompt.Create<T>(title, out cancelled, labels);

        private class MultiPrompt : Prompt
        {
            private ITuple output;
            private string[] labels;
            private object[] values;
            private Type[] types;
            
            public static T GetResult<T>(string title, string confirmButton, out bool cancelled, params string[] labels)
            {
                MultiPrompt prompt = CreateInstance<MultiPrompt>();
                prompt.Title = title;
                prompt.labels = labels;
                prompt.ConfirmButton = confirmButton;
                prompt.output = (ITuple) Activator.CreateInstance<T>();
                prompt.OnInitialize();

                prompt.titleContent = new GUIContent(title);
                prompt.IsInitialized = true;

                EditorUtil.CenterWindow(prompt);
                prompt.position = new Rect(prompt.position.position, promptSize);
                prompt.ShowModalUtility();
                prompt.IsInitialized = false;

                cancelled = prompt.Cancelled;
                return (T) prompt.output;
            }

            protected override void DrawFields()
            {
                for(int i = 0; i < this.values.Length; i++)
                {
                    object value = this.values[i];
                    Type type = this.types[i];
                    this.values[i] = DrawField(value, type, this.labels[i]);
                }
            }
            
            protected override void OnConfirm() => ApplyOutput();
            
            protected override void OnCancel() => ApplyOutput();

            protected override void OnInitialize()
            {
                int numElements = this.output.Length;
                
                // If there are not enough elements in the labels array, fill the rest with empty strings.
                string[] labelsPadded = new string[numElements];
                for(int i = 0;i < labelsPadded.Length;i++)
                {
                    if(i < this.labels.Length)
                        labelsPadded[i] = this.labels[i];
                    else
                        labelsPadded[i] = "";
                }

                this.labels = labelsPadded;
                this.values = new object[numElements];
                this.types = new Type[numElements];
                for(int i = 0; i < numElements; i++)
                {
                    FieldInfo itemField = this.output.GetType().GetField("Item" + (i + 1), BindingFlags.Public | BindingFlags.Instance);
                    PropertyInfo itemProperty = this.output.GetType().GetProperty("Item" + (i + 1), BindingFlags.Public | BindingFlags.Instance);

                    Type itemType = (itemField == null) ? itemProperty.PropertyType : itemField.FieldType;
                    object instance = null;
                    if(itemType.IsValueType)
                        instance = Activator.CreateInstance(itemType);

                    this.values[i] = instance;
                    this.types[i] = itemType;
                }
            }

            private void ApplyOutput()
            {
                for(int i = 0; i < this.output.Length; i++)
                {
                    this.output.GetType().GetField("Item" + (i + 1), BindingFlags.Public | BindingFlags.Instance).SetValue(this.output, this.values[i]);
                }
            }
        }

        private class SinglePrompt : Prompt
        {
            private string label;
            private object output;
            private Type type;

            protected override void DrawFields() => this.output = DrawField(this.output, this.type, this.label);

            protected override void OnInitialize() { }

            public static T GetResult<T>(string title, string confirmButton, out bool cancelled, string label)
            {
                SinglePrompt prompt = CreateInstance<SinglePrompt>();
                prompt.Title = title;
                prompt.label = label;
                prompt.ConfirmButton = confirmButton;
                prompt.type = typeof(T);
                prompt.OnInitialize();

                prompt.titleContent = new GUIContent(title);
                prompt.IsInitialized = true;

                EditorUtil.CenterWindow(prompt);
                prompt.position = new Rect(prompt.position.position, promptSize);
                prompt.ShowModalUtility();
                prompt.IsInitialized = false;

                cancelled = prompt.Cancelled;
                return (T) prompt.output;
            }
        }
    }
}
