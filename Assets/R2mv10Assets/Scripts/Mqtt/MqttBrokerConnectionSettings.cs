using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;

namespace R2mv.Mqtt
{
    
    [CreateAssetMenu(menuName = "Road to the Metaverse/MQTT Broker Connection Settings")]
    public class MqttBrokerConnectionSettings : ScriptableObject
    {
        
        [Header("MQTT broker configuration")]
        [Tooltip("IP address or URL of the host running the broker")]
        public string brokerAddress = "localhost";
        [Tooltip("Port where the broker accepts connections")]
        public int brokerPort = 1883;
        [Tooltip("Use encrypted connection")]
        public bool isEncrypted = false;
        [Header("Connection parameters")]
        [Tooltip("Connection to the broker is delayed by the the given milliseconds")]
        public int connectionDelay = 500;
        [Tooltip("Connection timeout in milliseconds")]
        public int timeoutOnConnection = MqttSettings.MQTT_CONNECT_TIMEOUT;
        [Tooltip("Connect on startup")]
        public bool autoConnect = false;
        [Tooltip("UserName for the MQTT broker. Keep blank if no user name is required.")]
        public string userName = null;
        [Tooltip("Password for the MQTT broker. Keep blank if no password is required.")]
        public string password = null;
        [Header("MQTT Topics")] 
        [Tooltip("List of topics to subscribe to")]
        public string[] topics;
    }
}