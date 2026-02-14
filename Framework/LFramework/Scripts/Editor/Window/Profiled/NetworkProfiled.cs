using GameFramework;
using GameFramework.Network;
using UnityEditor;
using UnityEngine;
using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    /*
    internal sealed class NetworkProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private NetworkComponent _networkComponent;

        internal override void Draw()
        {
            GetComponent(ref _networkComponent);
            EditorGUILayout.LabelField("Network Channel Count", _networkComponent.NetworkChannelCount.ToString());

            INetworkChannel[] networkChannels = _networkComponent.GetAllNetworkChannels();
            foreach (INetworkChannel networkChannel in networkChannels)
            {
                DrawNetworkChannel(networkChannel);
            }
        }


        private void DrawNetworkChannel(INetworkChannel networkChannel)
        {
            EditorGUILayout.BeginVertical("box");
            {
                EditorGUILayout.LabelField(networkChannel.Name,
                    networkChannel.Connected ? "Connected" : "Disconnected");
                EditorGUILayout.LabelField("Service Type", networkChannel.ServiceType.ToString());
                EditorGUILayout.LabelField("Address Family", networkChannel.AddressFamily.ToString());
                EditorGUILayout.LabelField("Local Address",
                    networkChannel.Connected ? networkChannel.Socket.LocalEndPoint.ToString() : "Unavailable");
                EditorGUILayout.LabelField("Remote Address",
                    networkChannel.Connected ? networkChannel.Socket.RemoteEndPoint.ToString() : "Unavailable");
                EditorGUILayout.LabelField("Send Packet",
                    Utility.Text.Format("{0} / {1}", networkChannel.SendPacketCount, networkChannel.SentPacketCount));
                EditorGUILayout.LabelField("Receive Packet",
                    Utility.Text.Format("{0} / {1}", networkChannel.ReceivePacketCount,
                        networkChannel.ReceivedPacketCount));
                EditorGUILayout.LabelField("Miss Heart Beat Count", networkChannel.MissHeartBeatCount.ToString());
                EditorGUILayout.LabelField("Heart Beat",
                    Utility.Text.Format("{0:F2} / {1:F2}", networkChannel.HeartBeatElapseSeconds,
                        networkChannel.HeartBeatInterval));
                EditorGUI.BeginDisabledGroup(!networkChannel.Connected);
                {
                    if (GUILayout.Button("Disconnect"))
                    {
                        networkChannel.Close();
                    }
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndVertical();

            EditorGUILayout.Separator();
        }
    }
    */
}