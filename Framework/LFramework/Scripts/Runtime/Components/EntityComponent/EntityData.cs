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

        [SerializeField] private Vector3? m_Position = null;

        [SerializeField] private Quaternion? m_Rotation = null;

        [SerializeField] private Vector3? m_Size = null;

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
            get { return m_Position; }
            set { m_Position = value; }
        }

        /// <summary>
        /// 实体朝向。
        /// </summary>
        public Quaternion? Rotation
        {
            get { return m_Rotation; }
            set { m_Rotation = value; }
        }

        public Vector3? Size
        {
            get => m_Size;
            set => m_Size = value;
        }

        public object UserData
        {
            get => m_UserData;
            set => m_UserData = value;
        }

        public bool ForceUseFullEntityPath { get; set; }

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