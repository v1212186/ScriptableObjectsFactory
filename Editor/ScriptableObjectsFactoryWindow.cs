using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using PopupWindow = UnityEngine.UIElements.PopupWindow;

//TODO собственный элемент для холда типа СО и strings xml файл для названий
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

        ScrollView scriptalbeObjectsScrollView;

        private string selectedAssembly;

        private Dictionary<string, Type> scriptableObjects;

        private string selectedScriptableObject;

        private bool isInitialized;

        [MenuItem("Tools/Scriptable Objects Factory")]
        public static void ShowExample()
        {
            ScriptableObjectsFactoryWindow window = GetWindow<ScriptableObjectsFactoryWindow>("Scriptable Objects Factory");
            window.minSize = new Vector2(250, 300);
        }

        public void OnEnable()
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
            ToolbarSearchField toolbarSearchField = _root.Q<ToolbarSearchField>("toolbarSearch");
            toolbarSearchField.RegisterValueChangedCallback(OnSearchTextChanged);

            PopupWindow popupWindow = _root.Q<PopupWindow>("popupWindow");
            List<string> assemblyNames = AssemblyUtils.GetAssemblyNames(AssemblyUtils.GetPlayerAssemblies()).ToList();
            PopupField<string> popupField = new PopupField<string>(assemblyNames, 0, OnEnumPopupSelected);
            popupWindow.Add(popupField);

            scriptalbeObjectsScrollView = _root.Q<ScrollView>("scrollView");

            Button createButton = _root.Q<Button>("createButton");
            createButton.clickable.clicked += CreateScriptableObject;
        }

        private void CreateLayoutManually(VisualElement _root)
        {
            Toolbar toolbar = new Toolbar();
            _root.Add(toolbar);

            ToolbarSearchField searchField = new ToolbarSearchField();
            toolbar.Add(searchField);
            searchField.RegisterValueChangedCallback(OnSearchTextChanged);

            PopupWindow popupWindow = new PopupWindow();
            popupWindow.text = "Assemblies";
            _root.Add(popupWindow);

            List<string> assemblyNames = AssemblyUtils.GetAssemblyNames(AssemblyUtils.GetPlayerAssemblies()).ToList();
            PopupField<string> popupField = new PopupField<string>(assemblyNames, 0, OnEnumPopupSelected);
            popupWindow.Add(popupField);

            Box scriptalbeObjectsContainer = new Box();
            scriptalbeObjectsScrollView = new ScrollView { showHorizontal = false };
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
            Button button = new Button(CreateScriptableObject) { text = "Create scriptable object" };
            buttonContainer.Add(button);
            _root.Add(buttonContainer);
        }

        private void RefreshScriptableObjectElements()
        {
            scriptalbeObjectsScrollView.Clear();
            scriptableObjects.Clear();

            GetScriptableObjectsNames();

            foreach (string scriptableObjectsKey in scriptableObjects.Keys)
            {
                scriptalbeObjectsScrollView.Add(CreateScriptalbeObjectElement(scriptableObjectsKey));
            }
        }

        public void CreateScriptableObject()
        {
            var asset = ScriptableObject.CreateInstance(scriptableObjects[selectedScriptableObject]);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(
                asset.GetInstanceID(),
                ScriptableObject.CreateInstance<EndNameEdit>(),
                $"{scriptableObjects[selectedScriptableObject]}.asset",
                AssetPreview.GetMiniThumbnail(asset),
                null);
        }

        public VisualElement CreateScriptalbeObjectElement(string _name)
        {
            Label task = new Label(_name) { focusable = true, name = _name };
            task.AddManipulator(new Clickable(OnElementClick));
            task.AddToClassList("task");

            task.RegisterCallback<KeyDownEvent, string>(ConfirmButtonPress, task.name);

            return task;
        }

        private void OnElementClick(EventBase _obj)
        {
            Label clickedLabel = _obj.target as Label;
            if (clickedLabel == null)
            {
                Debug.LogError("Clicked on null label");
                return;
            }

            selectedScriptableObject = clickedLabel.text;
        }

        public void ConfirmButtonPress(KeyDownEvent e, string scriptableObjectName)
        {
            if (e.keyCode == KeyCode.Return)
            {
                if (scriptableObjectName != null)
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

        private void OnSearchTextChanged(ChangeEvent<string> evt)
        {
            Debug.Log(evt.newValue);
        }

        #region Scriptable objects

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