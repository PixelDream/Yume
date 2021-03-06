﻿#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
using CrabEditor;
#endif
using System.Collections.Generic;
using System.Linq;

namespace Crab.Events
{
    using UnityEngine;

    public class Trigger : MonoBehaviour
    {
        //Static
        public static HashSet<Trigger> triggersReady = new HashSet<Trigger>();
        
        
        [System.NonSerialized]
        public Collider tCollider;

        public List<Crab.Event> eventsFired = new List<Crab.Event>();
        public List<Crab.Event> eventsFinished = new List<Crab.Event>();
        public List<Crab.Event> eventsEnabled = new List<Crab.Event>();
        public Vector3 size;
        public LayerMask affectedLayers;

        public bool firedByColliders = true;
        public bool checkTouchTarget = false;
        public bool debug = true;

        void OnEnable() {
            tCollider = GetComponent<Collider>();

            tCollider.isTrigger = true;
            tCollider.enabled = true;
        }

        void OnTriggerEnter(Collider col)
        {
            if (!triggersReady.Contains(this) && checkTouchTarget)
                triggersReady.Add(this);

            if (firedByColliders && IsInLayerMask(col.gameObject, affectedLayers)) {
                Fire();
            }
        }

        void OnTriggerExit(Collider col)
        {
            eventsFinished.ForEach(x => {
                if (x && x.isActiveAndEnabled) x.SendMessage("StartEvent");
            });

            triggersReady.Remove(this);
        }

        void OnDestroy()
        {
            triggersReady.Remove(this);
        }

        //Start all the events
        public void Fire()
        {
            eventsFired.ForEach(x => {
                if (x && x.isActiveAndEnabled &&
                    (!checkTouchTarget || x == Cache.Get.player.touchTarget))
                {
                    x.SendMessage("StartEvent");
                }
            });
            eventsEnabled.ForEach(x => {
                if (x && x.isActiveAndEnabled) x.Enable();
            });
        }

        
        //Handlers
        private bool IsInLayerMask(GameObject obj, LayerMask layerMask) {
            return (layerMask.value & (1 << obj.layer)) > 0;
        }
        

        //Render Event Connections
        void OnDrawGizmos() {
            if (!debug)
                return;

            Color gizmosColor = Gizmos.color;

            Gizmos.color = Color.red;
            eventsFinished.ForEach(x => {
                if (x) Gizmos.DrawLine(transform.position, x.transform.position);
            });

            Gizmos.color = Color.blue;
            eventsEnabled.ForEach(x => {
                if (x) Gizmos.DrawLine(transform.position, x.transform.position);
            });

            Gizmos.color = Color.green;
            eventsFired.ForEach(x => {
                if (x) Gizmos.DrawLine(transform.position, x.transform.position);
            });

            Gizmos.color = gizmosColor;
        }
    }



    #if UNITY_EDITOR
    [CustomEditor(typeof(Trigger))]
    public class TriggerEditor : Editor
    {
        protected Trigger t;
        private ReorderableList firedEvents;
        private ReorderableList finishedEvents;
        private ReorderableList enabledEvents;

        private void OnEnable() {
            firedEvents = new ReorderableList(serializedObject,
                    serializedObject.FindProperty("eventsFired"),
                    true, true, true, true);
            firedEvents.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Started Events");
            };
            firedEvents.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = firedEvents.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    element, GUIContent.none);
            };


            finishedEvents = new ReorderableList(serializedObject,
                    serializedObject.FindProperty("eventsFinished"),
                    true, true, true, true);
            finishedEvents.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Finished Events");
            };
            finishedEvents.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = finishedEvents.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    element, GUIContent.none);
            };


            enabledEvents = new ReorderableList(serializedObject,
                    serializedObject.FindProperty("eventsEnabled"),
                    true, true, true, true);
            enabledEvents.drawHeaderCallback = (Rect rect) => {
                EditorGUI.LabelField(rect, "Enabled Events");
            };
            enabledEvents.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) => {
                var element = enabledEvents.serializedProperty.GetArrayElementAtIndex(index);
                rect.y += 2;
                EditorGUI.PropertyField(
                    new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight),
                    element, GUIContent.none);
            };
        }

        void Awake()
        {
            t = target as Trigger;

            if (t.firedByColliders)
            {
                //Find Collider or Create it
                t.tCollider = t.GetComponent<Collider>();
                UpdateCollider();
                t.tCollider.isTrigger = true;
            }
        }

        public override void OnInspectorGUI() {
            serializedObject.Update();

            EditorGUILayout.LabelField("Affected Layers");
            t.affectedLayers = Util.LayerMaskField(t.affectedLayers);

            UpdateGUI();

            
            firedEvents.DoLayoutList();
            finishedEvents.DoLayoutList();
            enabledEvents.DoLayoutList();

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            t.firedByColliders = EditorGUILayout.Toggle("Fired By Colliders", t.firedByColliders);
            t.checkTouchTarget = EditorGUILayout.Toggle("Check Touch Target", t.checkTouchTarget);
            t.debug = EditorGUILayout.Toggle("Debug", t.debug);
            EditorGUILayout.LabelField("Don't edit the collider.", EditorStyles.boldLabel);

            serializedObject.ApplyModifiedProperties();

            if (GUI.changed)
            {
                if(t.firedByColliders)
                {
                    t.tCollider.isTrigger = true;
                    UpdateCollider();
                }

                EditorUtility.SetDirty(target);
            }
        }

        protected virtual void UpdateGUI() {
            EditorGUILayout.LabelField("Size");
            t.size = EditorGUILayout.Vector3Field("", t.size);
        }

        protected virtual void UpdateCollider()
        {
            if (!t.tCollider)
            {
                t.tCollider = t.gameObject.AddComponent<BoxCollider>();
            }

            BoxCollider collider = t.tCollider as BoxCollider;
            if (collider) {
                collider.size = t.size;
            }
        }
    }
    #endif
}