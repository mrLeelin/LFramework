using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LFramework.Runtime
{
    [Serializable]
    public class EntityData
    {
        [SerializeField] private int m_Id = 0;

        [SerializeField] private bool m_HasPosition = false;
        [SerializeField] private Vector3 m_Position = Vector3.zero;

        [SerializeField] private bool m_HasRotation = false;
        [SerializeField] private Quaternion m_Rotation = Quaternion.identity;

        [SerializeField] private bool m_HasSize = false;
        [SerializeField] private Vector3 m_Size = Vector3.one;

        [SerializeField] private string m_EntityAssetsPath;

        [SerializeField] private bool m_IsFullEntityPath = false;

        private object m_UserData;


        public EntityData(string entityAssetsPath, int entityId = 0)
        {
            m_Id = entityId;
            m_EntityAssetsPath = entityAssetsPath;
        }

        /// <summary>
        /// 实体编号。
        /// </summary>
        public int Id
        {
            get { return m_Id; }
            set { m_Id = value; }
        }


        /// <summary>
        /// 实体位置。
        /// </summary>
        public Vector3? Position
        {
            get => m_HasPosition ? m_Position : null;
            set { m_HasPosition = value.HasValue; m_Position = value ?? Vector3.zero; }
        }

        /// <summary>
        /// 实体朝向。
        /// </summary>
        public Quaternion? Rotation
        {
            get => m_HasRotation ? m_Rotation : null;
            set { m_HasRotation = value.HasValue; m_Rotation = value ?? Quaternion.identity; }
        }

        public Vector3? Size
        {
            get => m_HasSize ? m_Size : null;
            set { m_HasSize = value.HasValue; m_Size = value ?? Vector3.one; }
        }

        public object UserData
        {
            get => m_UserData;
            set => m_UserData = value;
        }
        

        /// <summary>
        /// 实体资源地址
        /// </summary>
        public string EntityAssetsPath
        {
            get => m_EntityAssetsPath;
            set => m_EntityAssetsPath = value;
        }
    }
}