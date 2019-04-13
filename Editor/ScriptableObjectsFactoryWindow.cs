using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using Assembly = System.Reflection.Assembly;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

namespace ScriptableObjectsFactory.Editor
{
    internal class EndNameEdit : EndNameEditAction
    {
        public override void Action(int _instanceId, string _pathName, string _resourceFile)
        {
            AssetDatabase.CreateAsset(EditorUtility.InstanceIDToObject(_instanceId),
                AssetDatabase.GenerateUniqueAssetPath(_pathName));
        }
    }
    
    public class ScriptableObjectsFactoryWindow : EditorWindow
    {
        private const string LayoutResourcePath = "ScriptableObjectsFactoryLayout";
        private const string StyleResourcePath = "ScriptableObjectsFactoryStyle";
        private const int WindowMinSizeX = 250;
        private const int WindowMinSizeY = 185;
        private const string WindowTitle = "Scriptable Objects Factory";
        
        private ScrollView scriptalbeObjectsScrollView;

        private string selectedAssembly;

        private Dictionary<string, Type> scriptableObjects;

        private string selectedScriptableObject;

        private bool isInitialized;

        [MenuItem("Tools/"+WindowTitle)]
        public static void ShowExample()
        {
            ScriptableObjectsFactoryWindow window =
                GetWindow<ScriptableObjectsFactoryWindow>(WindowTitle);
            window.minSize = new Vector2(WindowMinSizeX, WindowMinSizeY);
        }

        private void OnEnable()
        {
            scriptableObjects = new Dictionary<string, Type>();
            isInitialized = false;

            VisualElement root = rootVisualElement;
            StyleSheet styleSheet = Resources.Load<StyleSheet>(StyleResourcePath);
            VisualTreeAsset visualTree = Resources.Load<VisualTreeAsset>(LayoutResourcePath);
            visualTree.CloneTree(root);
            root.styleSheets.Add(styleSheet);

            RegisterCallbacks(root);
            RefreshScriptableObjectElements();

            isInitialized = true;
        }

        private void RegisterCallbacks(VisualElement _root)
        {
            PopupWindow popupWindow = _root.Q<PopupWindow>("popupWindow");
            List<string> assemblyNames = AssemblyUtils.GetAssemblyNames(AssemblyUtils.GetAssembliesByType(AssembliesType.Player)).ToList();
            PopupField<string> popupField = new PopupField<string>(assemblyNames, 0, OnEnumPopupSelected);
            popupWindow.Add(popupField);

            scriptalbeObjectsScrollView = _root.Q<ScrollView>("scrollView");

            Button createButton = _root.Q<Button>("createButton");
            createButton.clickable.clicked += CreateScriptableObject;
        }

        private void CreateLayoutManually(VisualElement _root)
        {
            PopupWindow popupWindow = new PopupWindow();
            popupWindow.text = "Assemblies";
            _root.Add(popupWindow);

            List<string> assemblyNames = AssemblyUtils.GetAssemblyNames(AssemblyUtils.GetAssembliesByType(AssembliesType.Player)).ToList();
            PopupField<string> popupField = new PopupField<string>(assemblyNames, 0, OnEnumPopupSelected);
            popupWindow.Add(popupField);

            Box scriptalbeObjectsContainer = new Box();
            scriptalbeObjectsScrollView = new ScrollView {showHorizontal = false};
            scriptalbeObjectsContainer.Add(scriptalbeObjectsScrollView);
            _root.Add(scriptalbeObjectsContainer);

            RefreshScriptableObjectElements();

            VisualElement buttonContainer = new VisualElement()
            {
                style =
                {
                    marginBottom = 10,
                    marginLeft = 10,
                    marginRight = 10,
                }
            };
            Button button = new Button(CreateScriptableObject) {text = "Create scriptable object"};
            buttonContainer.Add(button);
            _root.Add(buttonContainer);
        }

        private void RefreshScriptableObjectElements()
        {
            scriptalbeObjectsScrollView.Clear();
            scriptableObjects.Clear();
            selectedScriptableObject = null;
            
            GetScriptableObjectsNames();

            foreach (string scriptableObjectsKey in scriptableObjects.Keys)
            {
                scriptalbeObjectsScrollView.Add(CreateScriptalbeObjectElement(scriptableObjectsKey));
            }
        }

        private void CreateScriptableObject()
        {
            if (string.IsNullOrEmpty(selectedScriptableObject) || !scriptableObjects.ContainsKey(selectedScriptableObject))
            {
                return;
            }
            
            var asset = ScriptableObject.CreateInstance(scriptableObjects[selectedScriptableObject]);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                ScriptableObject.CreateInstance<EndNameEdit>(),
                $"{scriptableObjects[selectedScriptableObject]}.asset",
                AssetPreview.GetMiniThumbnail(asset),
                null);

            selectedScriptableObject = null;
        }

        private VisualElement CreateScriptalbeObjectElement(string _name)
        {
            Label task = new Label(_name) {focusable = true, name = _name};
            task.AddToClassList("task");

            task.RegisterCallback<KeyDownEvent, string>(ConfirmButtonPress, task.name);
            task.RegisterCallback<FocusInEvent, string>(OnElementFocusIn, task.name);

            return task;
        }
        
        private void OnElementFocusIn(FocusInEvent _focusEvent, string _scriptableObjectName)
        {
            selectedScriptableObject = _scriptableObjectName;
        }

        private void ConfirmButtonPress(KeyDownEvent _keyDownEvent, string _scriptableObjectName)
        {
            if (_keyDownEvent.keyCode == KeyCode.Return)
            {
                if (_scriptableObjectName != null)
                {
                    CreateScriptableObject();
                }
            }
        }

        private string OnEnumPopupSelected(string _assemblyName)
        {
            selectedAssembly = _assemblyName;
            if (isInitialized)
            {
                RefreshScriptableObjectElements();
            }

            return _assemblyName;
        }

        #region Assemblies

        private void GetScriptableObjectsNames()
        {
            Assembly assembly = AssemblyUtils.GetAssembly(selectedAssembly);
            Type[] scriptalbeObjectTypes = GetAllScriptableObjects(assembly);
            string[] scriptableObjectNames = scriptalbeObjectTypes.Select(_t => _t.FullName).ToArray();

            for (int i = 0; i < scriptableObjectNames.Length; i++)
            {
                scriptableObjects.Add(scriptableObjectNames[i], scriptalbeObjectTypes[i]);
            }
        }

        private Type[] GetAllScriptableObjects(Assembly _assembly)
        {
            Type[] allScriptableObjects = (from t in _assembly.GetTypes()
                where t.IsSubclassOf(typeof(ScriptableObject))
                select t).ToArray();

            return allScriptableObjects;
        }

        #endregion
    }
}