﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.Azure.RemoteRendering;
using Microsoft.Azure.RemoteRendering.Unity;
using System.Collections.Generic;
using UnityEngine;

public class RemoteObjectReset : MonoBehaviour
{
    private IEnumerable<EntitySnapshot> _originalState = null;

    #region Serialized Fields
    [SerializeField]
    [Tooltip("The transform containing the root entity.")]
    private Transform root = null;

    /// <summary>
    /// Get or set the transform containing the root entity
    /// </summary>
    public Transform Root
    {
        get => root;
        set
        {
            if (root != value)
            {
                root = value;
                CaptureInitialStateOnce();
            }
        }
    }

    [Header("Events")]

    [SerializeField]
    [Tooltip("Called when ResetObject has completed.")]
    private RemoteObjectResetCompletedEvent resetCompleted = new RemoteObjectResetCompletedEvent();

    /// <summary>
    /// Called when ResetObject has completed.
    /// </summary>
    public RemoteObjectResetCompletedEvent ResetCompleted
    {
        get => resetCompleted;
    }
    #endregion Serialized Fields

    #region Public Properties
    /// <summary>
    /// Get the initial state of the remote object.
    /// </summary>
    public IEnumerable<EntitySnapshot> OriginalState
    {
        get
        {
            if (_originalState == null)
            {
                CaptureInitialStateOnce();
            }
            return _originalState;
        }
    }
    #endregion Public Properties

    #region MonoBehavior Methods
    private void Start()
    {
        CaptureInitialStateOnce();
    }
    #endregion MonoBehavior Methods

    #region Public Methods
    /// <summary>
    /// Call this once a remote object is loaded. This gets the root object, and save it's state.
    /// </summary>
    /// <remarks>This is called via an event binding in the inspector window.</remarks>
    public void InitializeObject(RemoteObjectLoadedEventData data)
    {
        Root = data?.SyncObject?.transform;
    }

    /// <summary>
    /// Reset to the original Entity state
    /// </summary>
    public void ResetObject()
    {
        ResetObject(true);
    }

    /// <summary>
    /// Reset to the original Entity state
    /// </summary>
    public void ResetObject(bool resetMaterials)
    {
        foreach (var state in _originalState)
        {
            Entity entity = state.Entity;
            if (entity != null && entity.Valid)
            {
                if (resetMaterials)
                {
                    entity.ReplaceMaterials(null);
                }

                if (entity.Parent != state.Parent?.Entity) // this also filters out static entities that do not support reparenting
                {
                    entity.Parent = state.Parent?.Entity;
                }
                entity.Position = state.LocalPosition.toRemotePos();
                entity.Rotation = state.LocalRotation.toRemote();
                entity.Scale = state.LocalScale.toRemote();
            }
        }

        resetCompleted?.Invoke(new RemoteObjectResetCompletedEventData(this));
    }
    #endregion Public Methods

    #region Private Methods
    private void CaptureInitialStateOnce()
    {
        RemoteEntitySyncObject syncObject = null;
        if (root != null)
        {
            syncObject = root.GetComponent<RemoteEntitySyncObject>();
        }

        if (syncObject == null || !syncObject.IsEntityValid)
        {
            _originalState = null;
        }
        else
        {
            _originalState = syncObject.Entity.CreateSnapshot();
        }
    }
    #endregion Private Methods
}
