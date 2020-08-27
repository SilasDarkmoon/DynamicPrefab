using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using Capstones.UnityEngineEx;
using Capstones.UnityEditorEx;

[CustomEditor(typeof(DynamicImage))]
public class DynamicImageInspector : InspectorBase<DynamicImage>
{
    private SerializedObject soTarget;
    private SerializedProperty spPath;
    private SerializedProperty spOnlyEmpty;
    private SerializedProperty spImageType;
    private SerializedProperty spPreserveAspect;
    private SerializedProperty spFillMethod;
    private SerializedProperty spFillAmount;
    private SerializedProperty spIsNativeSize;
    private float lableXMin = 15.0f;
    private int controlXMin;

    public void OnEnable()
    {
        soTarget = new SerializedObject(target);
        spPath = soTarget.FindProperty("Path");
        spOnlyEmpty = soTarget.FindProperty("OnlyLoadWhenEmpty");
        spImageType = soTarget.FindProperty("ImageType");
        spPreserveAspect = soTarget.FindProperty("PreserveAspect");
        spFillMethod = soTarget.FindProperty("FillMethod");
        spFillAmount = soTarget.FindProperty("FillAmount");
        spIsNativeSize = soTarget.FindProperty("IsNativeSize");
    }

    public override void OnInspectorGUI()
    {
        soTarget.Update();
        Rect controlRect = EditorGUILayout.GetControlRect(false, 0);
        controlXMin = (controlRect.width - 337) > 0 ? Mathf.RoundToInt((controlRect.width - 337) * 0.45f) + 134 : 134;
        soTarget.ApplyModifiedProperties();

        var oldp = Target.Path;
        EditorGUILayout.PropertyField(spPath);
        soTarget.ApplyModifiedProperties();
        if (oldp != Target.Path)
        {
            if (!string.IsNullOrEmpty(Target.Path))
            {
                soTarget.ApplyModifiedProperties();
            }
            Target.ApplySource();
            soTarget.Update();
        }

        var oldImageType = Target.ImageType;
        EditorGUILayout.PropertyField(spImageType);
        soTarget.ApplyModifiedProperties();
        if (oldImageType != Target.ImageType)
        {
            Target.ApplySource();
            soTarget.Update();
        }
        EditorGUILayout.BeginVertical();
        if (Target.ImageType == Image.Type.Filled)
        {
            var oldFillMethod = Target.FillMethod;
            Rect fillMethodLabelRect = EditorGUILayout.GetControlRect();
            Rect fillMethodControlRect = fillMethodLabelRect;
            fillMethodLabelRect.xMin += lableXMin;
            fillMethodControlRect.xMin = controlXMin;
            EditorGUI.LabelField(fillMethodLabelRect, "Fill Method");
            Target.FillMethod = (Image.FillMethod)EditorGUI.EnumPopup(fillMethodControlRect, Target.FillMethod);

            var oldFillOrigin = Target.FillOrigin;
            Rect fillOriginLabelRect = EditorGUILayout.GetControlRect();
            Rect fillOriginControlRect = fillOriginLabelRect;
            fillOriginLabelRect.xMin += lableXMin;
            fillOriginControlRect.xMin = controlXMin;
            EditorGUI.LabelField(fillOriginLabelRect, "Fill Origin");
            if (Target.FillMethod == Image.FillMethod.Horizontal)
            {
                Target.FillOrigin = (int)(Image.OriginHorizontal)EditorGUI.EnumPopup(fillOriginControlRect, (Image.OriginHorizontal)Target.FillOrigin);
            }
            else if (Target.FillMethod == Image.FillMethod.Vertical)
            {
                Target.FillOrigin = (int)(Image.OriginVertical)EditorGUI.EnumPopup(fillOriginControlRect, (Image.OriginVertical)Target.FillOrigin);
            }
            else if (Target.FillMethod == Image.FillMethod.Radial180)
            {
                Target.FillOrigin = (int)(Image.Origin180)EditorGUI.EnumPopup(fillOriginControlRect, (Image.Origin180)Target.FillOrigin);
            }
            else if (Target.FillMethod == Image.FillMethod.Radial360)
            {
                Target.FillOrigin = (int)(Image.Origin360)EditorGUI.EnumPopup(fillOriginControlRect, (Image.Origin360)Target.FillOrigin);
            }
            else if (Target.FillMethod == Image.FillMethod.Radial90)
            {
                Target.FillOrigin = (int)(Image.Origin90)EditorGUI.EnumPopup(fillOriginControlRect, (Image.Origin90)Target.FillOrigin);
            }
            
            var oldFillAmount = Target.FillAmount;
            Rect fillAmountLabelRect = EditorGUILayout.GetControlRect();
            Rect fillAmountControlRect = fillAmountLabelRect;
            fillAmountLabelRect.xMin += lableXMin;
            fillAmountControlRect.xMin = controlXMin;
            EditorGUI.LabelField(fillAmountLabelRect, "Fill Amount");
            Target.FillAmount = EditorGUI.Slider(fillAmountControlRect, Target.FillAmount, 0, 1);

            if (Target.FillMethod == Image.FillMethod.Radial90 || Target.FillMethod == Image.FillMethod.Radial180 || Target.FillMethod == Image.FillMethod.Radial360)
            {
                var oldFillClockwise = Target.FillClockwise;
                Rect fillClockwiseLabelRect = EditorGUILayout.GetControlRect();
                Rect fillClockwiseControlRect = fillClockwiseLabelRect;
                fillClockwiseLabelRect.xMin += lableXMin;
                fillClockwiseControlRect.xMin = controlXMin;
                EditorGUI.LabelField(fillClockwiseLabelRect, "Clockwise");
                Target.FillClockwise = EditorGUI.Toggle(fillClockwiseControlRect, Target.FillClockwise);
                soTarget.ApplyModifiedProperties();
                if (oldFillClockwise != Target.FillClockwise)
                {
                    Target.ApplySource();
                    soTarget.Update();
                }
            }

            soTarget.ApplyModifiedProperties();
            if (oldFillMethod != Target.FillMethod)
            {
                Target.FillOrigin = 0;
            }
            if (oldFillMethod != Target.FillMethod || oldFillOrigin != Target.FillOrigin || oldFillAmount != Target.FillAmount)
            {
                Target.ApplySource();
                Target.RebuildImage();
                soTarget.Update();
            }
        }
        if (Target.ImageType == Image.Type.Simple || Target.ImageType == Image.Type.Filled)
        {
            var oldPreserveAspect = Target.PreserveAspect;
            Rect preserveAspectLabelRect = EditorGUILayout.GetControlRect();
            Rect preserveAspectControlRect = preserveAspectLabelRect;
            preserveAspectLabelRect.xMin += lableXMin;
            preserveAspectControlRect.xMin = controlXMin;
            EditorGUI.LabelField(preserveAspectLabelRect, "Preserve Aspect");
            Target.PreserveAspect = EditorGUI.Toggle(preserveAspectControlRect, Target.PreserveAspect);
            soTarget.ApplyModifiedProperties();
            if (oldPreserveAspect != Target.PreserveAspect)
            {
                Target.ApplySource();
                soTarget.Update();
            }
            Rect buttonRect = EditorGUILayout.GetControlRect();
            buttonRect.xMin = controlXMin;
            if (GUI.Button(buttonRect, "Set Native Size"))
            {
                Target.SetNativeSize();
            }
        }
        else
        {
            var oldFillCenter = Target.FillCenter;
            Rect fillCenterLabelRect = EditorGUILayout.GetControlRect();
            Rect fillCenterControlRect = fillCenterLabelRect;
            fillCenterLabelRect.xMin += lableXMin;
            fillCenterControlRect.xMin = controlXMin;
            EditorGUI.LabelField(fillCenterLabelRect, "Fill Center");
            Target.FillCenter = EditorGUI.Toggle(fillCenterControlRect, Target.FillCenter);
            soTarget.ApplyModifiedProperties();
            if (oldFillCenter != Target.FillCenter)
            {
                Target.ApplySource();
                soTarget.Update();
            }
        }



        EditorGUILayout.EndVertical();

        EditorGUILayout.PropertyField(spOnlyEmpty);
        EditorGUILayout.PropertyField(spIsNativeSize);
        soTarget.ApplyModifiedProperties();
    }
}