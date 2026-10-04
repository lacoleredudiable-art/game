#if UNITY_EDITOR
using Dovus.Core.Boss;
using Dovus.Core.Element;
using Dovus.Core.Dodge;
using Dovus.Core.Damage;
using Dovus.Core.Casting;
using Dovus.Core.Input;
using Dovus.Core.Hud;
using Dovus.Core.Passives;
using Dovus.Core.Equipment;
using Dovus.Core.Grammar;
using Dovus.Core.Motion;
using Dovus.Core.Portal;
using Dovus.Core.Status;
using Dovus.Game.Actors;
using Dovus.Game.Boss;
using Dovus.Game.Casting;
using Dovus.Game.Composition;
using Dovus.Game.Data;
using Dovus.Game.Hud;
using Dovus.Game.Skills;
using Dovus.Game.Skills.Execution;
using Dovus.Game.Team;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.PlayerLoop;

namespace Dovus.Game.Editor
{
    public static partial class PlaySweep
    {
            // ---------------------------------------------------------------- yardımcılar

            static void CollectStateNames()
            {
                _stateNames.Clear();
                if (_animator == null)
                    return;
                RuntimeAnimatorController rc = _animator.runtimeAnimatorController;
                if (rc is AnimatorOverrideController oc)
                    rc = oc.runtimeAnimatorController;
                if (rc is not UnityEditor.Animations.AnimatorController ac)
                    return;
                foreach (var layer in ac.layers)
                    AddStates(layer.stateMachine);
            }

            static void AddStates(UnityEditor.Animations.AnimatorStateMachine sm)
            {
                foreach (var s in sm.states)
                    _stateNames[Animator.StringToHash(s.state.name)] = s.state.name;
                foreach (var child in sm.stateMachines)
                    AddStates(child.stateMachine);
            }

            static string StateName(int hash) => _stateNames.TryGetValue(hash, out string n) ? n : hash.ToString();

            static Vector3 Flat(Vector3 v)
            {
                v.y = 0f;
                return v;
            }

            static T F<T>(object o, string name)
            {
                object v = F(o, name);
                return v is T t ? t : default;
            }

            static object F(object o, string name)
            {
                for (Type t = o?.GetType(); t != null; t = t.BaseType)
                {
                    FieldInfo f = t.GetField(name, BF | BindingFlags.DeclaredOnly);
                    if (f != null)
                        return f.GetValue(o);
                }
                return null;
            }

            static T P<T>(object o, string name)
            {
                for (Type t = o?.GetType(); t != null; t = t.BaseType)
                {
                    PropertyInfo p = t.GetProperty(name, BF | BindingFlags.DeclaredOnly);
                    if (p != null)
                        return p.GetValue(o) is T v ? v : default;
                }
                return default;
            }

            static void S(object o, string name, object value)
            {
                for (Type t = o?.GetType(); t != null; t = t.BaseType)
                {
                    FieldInfo f = t.GetField(name, BF | BindingFlags.DeclaredOnly);
                    if (f != null)
                    {
                        f.SetValue(o, value);
                        return;
                    }
                }
            }

            static object Call(object o, string name, params object[] args)
            {
                for (Type t = o.GetType(); t != null; t = t.BaseType)
                {
                    foreach (MethodInfo m in t.GetMethods(BF | BindingFlags.DeclaredOnly))
                    {
                        if (m.Name == name && m.GetParameters().Length == args.Length)
                            return m.Invoke(o, args);
                    }
                }
                throw new MissingMethodException(o.GetType().Name, name);
            }

            static T Call<T>(object o, string name, params object[] args) =>
                Call(o, name, args) is T v ? v : default;
    }
}
#endif
