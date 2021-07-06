﻿// ----------------------------------------------------------------------------
// The MIT License
// InfiniteScroll https://github.com/mopsicus/infinite-scroll-unity
// Copyright (c) 2018 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

using UnityEditor;
using UnityEngine;

namespace Mopsicus.InfiniteScroll {

#if UNITY_EDITOR
    [CustomEditor (typeof (InfiniteScroll))]
	public class InfiniteScrollEditor : Editor {

		/// <summary>
		/// Scroller target
		/// </summary>
		private InfiniteScroll _target;

		/// <summary>
		/// Serialized target object
		/// </summary>
		private SerializedObject _object;

		/// <summary>
		/// Item list prefab
		/// </summary>
		private SerializedProperty _prefab;

		/// <summary>
		/// Top padding
		/// </summary>
		private SerializedProperty _topPadding;

		/// <summary>
		/// Bottom padding
		/// </summary>
		private SerializedProperty _bottomPadding;

		/// <summary>
		/// Spacing between items
		/// </summary>
		private SerializedProperty _itemSpacing;

		/// <summary>
		/// Pull top text label
		/// </summary>
		private SerializedProperty _topPullLabel;

		/// <summary>
		/// Release top text label
		/// </summary>
		private SerializedProperty _topReleaseLabel;

		/// <summary>
		/// Pull bottom text label
		/// </summary>
		private SerializedProperty _bottomPullLabel;

		/// <summary>
		/// Release bottom text label
		/// </summary>
		private SerializedProperty _bottomReleaseLabel;

		/// <summary>
		/// Can we pull from top
		/// </summary>
		private SerializedProperty _isPullTop;

		/// <summary>
		/// Can we pull from bottom
		/// </summary>
		private SerializedProperty _isPullBottom;

		/// <summary>
		/// Left padding
		/// </summary>
		private SerializedProperty _leftPadding;

		/// <summary>
		/// Right padding
		/// </summary>
		private SerializedProperty _rightPadding;

		/// <summary>
		/// Pull left text label
		/// </summary>
		private SerializedProperty _leftPullLabel;

		/// <summary>
		/// Release left text label
		/// </summary>
		private SerializedProperty _leftReleaseLabel;

		/// <summary>
		/// Pull right text label
		/// </summary>
		private SerializedProperty _rightPullLabel;

		/// <summary>
		/// Release right text label
		/// </summary>
		private SerializedProperty _rightReleaseLabel;

		/// <summary>
		/// Can we pull from left
		/// </summary>
		private SerializedProperty _isPullLeft;

		/// <summary>
		/// Can we pull from right
		/// </summary>
		private SerializedProperty _isPullRight;

		/// <summary>
		/// Should the content be centered if it fits in the screen
		/// </summary>
		private SerializedProperty _shouldCenterContentIfItFits;

		/// <summary>
		/// Init data
		/// </summary>
		private void OnEnable () {
			_target = (InfiniteScroll) target;
			_object = new SerializedObject (target);
			_prefab = _object.FindProperty ("Prefab");
			_topPadding = _object.FindProperty ("TopPadding");
			_bottomPadding = _object.FindProperty ("BottomPadding");
			_itemSpacing = _object.FindProperty ("ItemSpacing");
			_topPullLabel = _object.FindProperty ("TopPullLabel");
			_topReleaseLabel = _object.FindProperty ("TopReleaseLabel");
			_bottomPullLabel = _object.FindProperty ("BottomPullLabel");
			_bottomReleaseLabel = _object.FindProperty ("BottomReleaseLabel");
			_isPullTop = _object.FindProperty ("IsPullTop");
			_isPullBottom = _object.FindProperty ("IsPullBottom");
			_leftPadding = _object.FindProperty ("LeftPadding");
			_rightPadding = _object.FindProperty ("RightPadding");
			_leftPullLabel = _object.FindProperty ("LeftPullLabel");
			_leftReleaseLabel = _object.FindProperty ("LeftReleaseLabel");
			_rightPullLabel = _object.FindProperty ("RightPullLabel");
			_rightReleaseLabel = _object.FindProperty ("RightReleaseLabel");
			_isPullLeft = _object.FindProperty ("IsPullLeft");
			_isPullRight = _object.FindProperty ("IsPullRight");
			_shouldCenterContentIfItFits = _object.FindProperty("ShouldCenterContentIfItFits");
		}

		/// <summary>
		/// Draw inspector
		/// </summary>
		public override void OnInspectorGUI () {
			_object.Update ();
			EditorGUI.BeginChangeCheck ();
			_target.Type = GUILayout.Toolbar (_target.Type, new string[] { "Vertical", "Horizontal" });
			switch (_target.Type) {
				case 0:
					EditorGUILayout.PropertyField (_prefab);
					EditorGUILayout.PropertyField (_topPadding);
					EditorGUILayout.PropertyField (_bottomPadding);
					EditorGUILayout.PropertyField (_itemSpacing);
					EditorGUILayout.PropertyField (_topPullLabel);
					EditorGUILayout.PropertyField (_topReleaseLabel);
					EditorGUILayout.PropertyField (_bottomPullLabel);
					EditorGUILayout.PropertyField (_bottomReleaseLabel);
					EditorGUILayout.PropertyField (_isPullTop);
					EditorGUILayout.PropertyField (_isPullBottom);
					break;
				case 1:
					EditorGUILayout.PropertyField (_prefab);
					EditorGUILayout.PropertyField (_leftPadding);
					EditorGUILayout.PropertyField (_rightPadding);
					EditorGUILayout.PropertyField (_itemSpacing);
					EditorGUILayout.PropertyField (_leftPullLabel);
					EditorGUILayout.PropertyField (_leftReleaseLabel);
					EditorGUILayout.PropertyField (_rightPullLabel);
					EditorGUILayout.PropertyField (_rightReleaseLabel);
					EditorGUILayout.PropertyField (_isPullLeft);
					EditorGUILayout.PropertyField (_isPullRight);
					EditorGUILayout.PropertyField(_shouldCenterContentIfItFits);
					break;
				default:
					break;
			}
			if (EditorGUI.EndChangeCheck ()) {
				_object.ApplyModifiedProperties ();
			}
		}

	}
#endif
}