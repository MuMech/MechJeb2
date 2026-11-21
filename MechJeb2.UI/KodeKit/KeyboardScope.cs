using System;
using KodeKit.Headless;
using UnityEngine;
using UnityEngine.EventSystems;

namespace KodeKit
{
    /// <summary>
    /// A keyboard scope (<see cref="Headless.KeyboardScopes"/>): anything that takes the keyboard (a focused field, an open drop-down, a
    /// dialog, the command palette) owns one, on an object that lives as long as its owner. While the scope is open it holds KSP's
    /// <c>ControlTypes.KEYBOARDINPUT</c> lock under its own id, so a key typed there doesn't also fly the vessel; it lets the lock go once
    /// Escape and Enter are up (KSP pauses on Escape's key-up, FINDINGS rule 43). A scope that takes navigation turns uGUI's navigation
    /// events off while it is open (FINDINGS rule 54). The top scope gets the keys it routes (<see cref="Scope"/>'s
    /// <c>Key</c>/<c>Typed</c>/<c>Dismissed</c>); Escape closes the top scope only.
    /// <list type="bullet">
    /// <item><see cref="Lock"/> opens the scope, <see cref="UnlockAfterKeys"/> closes it, <see cref="Unlock"/> drops it at once.</item>
    /// <item>Disabled or destroyed (its window hidden or closed), the scope is dropped and its lock goes at once.</item>
    /// <item>Nothing runs per frame while no scope is open: one shared driver reads the keys, and only while a scope holds a lock.</item>
    /// </list>
    /// </summary>
    public class KeyboardScope : MonoBehaviour
    {
        /// <summary>The stack every KodeKit scope is on.</summary>
        public static readonly KeyboardScopes Scopes = MakeScopes();

        static KeyboardScopes MakeScopes()
        {
            var scopes = new KeyboardScopes(new UnityKeyboard());
            scopes.Changed += Apply;
            return scopes;
        }

        string lockId;
        KeyScope scope;

        /// <summary>The scope's KSP lock id.</summary>
        public string LockId => lockId ??= LockPrefix + GetInstanceID();

        protected virtual string LockPrefix => "KodeKitScope_";

        /// <summary>Whether the scope holds KSP's keyboard lock (open, or waiting for Escape and Enter to come up).</summary>
        public bool Locked => scope != null && scope.Locked;

        /// <summary>The scope on the stack: its routes, its key handlers.</summary>
        public KeyScope Scope => scope ?? Configure(ScopeKeys.None, false);

        /// <summary>Sets the keys the scope routes and whether it turns uGUI's navigation off (before its first <see cref="Lock"/>).</summary>
        public KeyScope Configure(ScopeKeys routes, bool takesNavigation)
        {
            if (scope != null && scope.Locked) scope.Drop();
            scope = Scopes.Create(this, routes, takesNavigation, () => this != null);
            string id = LockId;
            scope.LockChanged += on =>
            {
                if (on) InputLockManager.SetControlLock(ControlTypes.KEYBOARDINPUT, id);
                else InputLockManager.RemoveControlLock(id);
            };
            return scope;
        }

        /// <summary>Opens the scope on top: KSP's keyboard lock is taken now.</summary>
        public void Lock() => Scope.Open();

        /// <summary>Closes the scope and lets its lock go now.</summary>
        public void Unlock() => scope?.Drop();

        /// <summary>
        /// Closes the scope; its lock goes once the key that ended it is up. A field ends its edit when Escape goes down, and KSP opens
        /// its pause menu when Escape comes up: unlocked at once, cancelling an edit also pauses the game.
        /// </summary>
        public void UnlockAfterKeys()
        {
            // destroyed (its window closed before its owner let go): OnDestroy has dropped the scope already
            if (scope == null || this == null) return;
            // an owner being hidden or destroyed ends its edit from TMP's OnDisable, before this component's: its object is inactive
            // already, and it can't wait for keys
            if (!isActiveAndEnabled || !gameObject.activeInHierarchy) scope.Drop();
            else scope.Close();
        }

        protected virtual void OnDisable() => Unlock();

        protected virtual void OnDestroy() => Unlock();

        // ---- the shared driver and uGUI's navigation ----

        static Driver driver;
        static EventSystem navigationOn; // the event system whose navigation the scopes turned off
        static bool navigationWas;

        static void Apply()
        {
            bool busy = Scopes.LockWanted;
            if (driver == null && busy)
            {
                var go = new GameObject("KodeKitKeyboardScopes");
                DontDestroyOnLoad(go);
                driver = go.AddComponent<Driver>();
            }
            if (driver != null && driver.enabled != busy) driver.enabled = busy;

            bool off = Scopes.NavigationOff;
            if (off && navigationOn == null && EventSystem.current != null)
            {
                navigationOn = EventSystem.current;
                navigationWas = navigationOn.sendNavigationEvents;
                navigationOn.sendNavigationEvents = false;
            }
            else if (!off && navigationOn != null)
            {
                navigationOn.sendNavigationEvents = navigationWas;
                navigationOn = null;
            }
            else if (!off) navigationOn = null; // its event system is gone with its scene
        }

        /// <summary>Gives the keys to the top scope each frame, and lets the closed scopes' locks go after every update.</summary>
        sealed class Driver : MonoBehaviour
        {
            static readonly Func<string> typed = () => Input.inputString;

            void Update() => Scopes.Dispatch(UnityKeyboard.Down(), typed);

            void LateUpdate() => Scopes.Tick();
        }

        sealed class UnityKeyboard : IScopeKeyboard
        {
            public int Frame => Time.frameCount;

            public ScopeKeys Held
            {
                get
                {
                    ScopeKeys k = ScopeKeys.None;
                    if (Input.GetKey(KeyCode.UpArrow)) k |= ScopeKeys.Up;
                    if (Input.GetKey(KeyCode.DownArrow)) k |= ScopeKeys.Down;
                    if (Input.GetKey(KeyCode.PageUp)) k |= ScopeKeys.PageUp;
                    if (Input.GetKey(KeyCode.PageDown)) k |= ScopeKeys.PageDown;
                    if (Input.GetKey(KeyCode.Home)) k |= ScopeKeys.Home;
                    if (Input.GetKey(KeyCode.End)) k |= ScopeKeys.End;
                    if (Input.GetKey(KeyCode.Return) || Input.GetKey(KeyCode.KeypadEnter)) k |= ScopeKeys.Enter;
                    if (Input.GetKey(KeyCode.Escape)) k |= ScopeKeys.Escape;
                    if (Input.GetKey(KeyCode.LeftArrow)) k |= ScopeKeys.Left;
                    if (Input.GetKey(KeyCode.RightArrow)) k |= ScopeKeys.Right;
                    if (Input.GetKey(KeyCode.Tab)) k |= ScopeKeys.Tab;
                    return k;
                }
            }

            public static ScopeKeys Down()
            {
                ScopeKeys k = ScopeKeys.None;
                if (Input.GetKeyDown(KeyCode.UpArrow)) k |= ScopeKeys.Up;
                if (Input.GetKeyDown(KeyCode.DownArrow)) k |= ScopeKeys.Down;
                if (Input.GetKeyDown(KeyCode.PageUp)) k |= ScopeKeys.PageUp;
                if (Input.GetKeyDown(KeyCode.PageDown)) k |= ScopeKeys.PageDown;
                if (Input.GetKeyDown(KeyCode.Home)) k |= ScopeKeys.Home;
                if (Input.GetKeyDown(KeyCode.End)) k |= ScopeKeys.End;
                if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) k |= ScopeKeys.Enter;
                if (Input.GetKeyDown(KeyCode.Escape)) k |= ScopeKeys.Escape;
                if (Input.GetKeyDown(KeyCode.LeftArrow)) k |= ScopeKeys.Left;
                if (Input.GetKeyDown(KeyCode.RightArrow)) k |= ScopeKeys.Right;
                if (Input.GetKeyDown(KeyCode.Tab)) k |= ScopeKeys.Tab;
                return k;
            }
        }
    }
}
