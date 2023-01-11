using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace R2mv.Mqtt
{
    public class MqttBrokerConnectionManager : MonoBehaviour
    {
        [Header("MQTT broker configuration")] [Tooltip("IP address or URL of the host running the broker")]
        public MqttBrokerConnectionSettings connectionSettings;
        
        /// <summary>
        /// Wrapped MQTT client
        /// </summary>
        protected MqttClient client;

        private List<MqttMsgPublishEventArgs> messageQueue1 = new List<MqttMsgPublishEventArgs>();
        private List<MqttMsgPublishEventArgs> messageQueue2 = new List<MqttMsgPublishEventArgs>();
        private List<MqttMsgPublishEventArgs> frontMessageQueue = null;
        private List<MqttMsgPublishEventArgs> backMessageQueue = null;
        private bool mqttClientConnectionClosed = false;
        private bool mqttClientConnected = false;

        /// <summary>
        /// Event fired when a connection is successfully established
        /// </summary>
        public event Action ConnectionSucceeded;
        /// <summary>
        /// Event fired when failing to connect
        /// </summary>
        public event Action ConnectionFailed;

        public event Action<string, string> MessageDecoded; 

        /// <summary>
        /// Connect to the broker using current settings.
        /// </summary>
        public virtual void Connect()
        {
            if (client == null || !client.IsConnected)
            {
                StartCoroutine(DoConnect());
            }
        }

        /// <summary>
        /// Disconnect from the broker, if connected.
        /// </summary>
        public virtual void Disconnect()
        {
            if (client != null)
            {
                StartCoroutine(DoDisconnect());
            }
        }


        public virtual void Publish(string topic, string message)
        {
            if (client != null && client.IsConnected)
                client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
                false);
            else
                Debug.LogWarning("Failed to publish message: MQTT Client is null or not connected.");
        }
        
        
        public virtual void Publish(string topic, float message)
        {
            if (client != null && client.IsConnected)
                client.Publish(topic, BitConverter.GetBytes(message), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
                    false);
            else
                Debug.LogWarning("Failed to publish message: MQTT Client is null or not connected.");
        }

        /// <summary>
        /// Override this method to take some actions before connection (e.g. display a message)
        /// </summary>
        protected virtual void OnConnecting()
        {
            Debug.LogFormat("Connecting to broker on {0}:{1}...\n", connectionSettings.brokerAddress, connectionSettings.brokerPort.ToString());
        }

        /// <summary>
        /// Override this method to take some actions if the connection succeeded.
        /// </summary>
        protected virtual void OnConnected()
        {
            Debug.LogFormat("Connected to {0}:{1}...\n", connectionSettings.brokerAddress, connectionSettings.brokerPort.ToString());

            SubscribeTopics();

            ConnectionSucceeded?.Invoke();
        }

        /// <summary>
        /// Override this method to take some actions if the connection failed.
        /// </summary>
        protected virtual void OnConnectionFailed(string errorMessage)
        {
            Debug.LogWarning("Connection failed.");
            ConnectionFailed?.Invoke();
        }

        /// <summary>
        /// Override this method to subscribe to MQTT topics.
        /// </summary>
        protected virtual void SubscribeTopics()
        {
            // create and populate an array with default QoS Levels
            // Todo: this should be a struct or custom class, maybe?
            var qosLevels = new byte[connectionSettings.topics.Length];
            Array.Fill(qosLevels, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE);
            
            client.Subscribe(connectionSettings.topics, qosLevels);
        }

        /// <summary>
        /// Override this method to unsubscribe to MQTT topics (they should be the same you subscribed to with SubscribeTopics() ).
        /// </summary>
        protected virtual void UnsubscribeTopics()
        {
        }

        /// <summary>
        /// Disconnect before the application quits.
        /// </summary>
        protected virtual void OnApplicationQuit()
        {
            CloseConnection();
        }

        /// <summary>
        /// Initialize MQTT message queue
        /// Remember to call base.Awake() if you override this method.
        /// </summary>
        protected virtual void Awake()
        {
            frontMessageQueue = messageQueue1;
            backMessageQueue = messageQueue2;
        }

        /// <summary>
        /// Connect on startup if autoConnect is set to true.
        /// </summary>
        protected virtual void Start()
        {
            if (connectionSettings.autoConnect)
            {
                Connect();
            }
        }

        /// <summary>
        /// Override this method for each received message you need to process.
        /// </summary>
        protected virtual void DecodeMessage(string topic, byte[] message)
        {
            //Debug.LogFormat("Message received on topic: {0}", topic);
            MessageDecoded?.Invoke(topic, System.Text.Encoding.UTF8.GetString(message));
        }

        /// <summary>
        /// Override this method to take some actions when disconnected.
        /// </summary>
        protected virtual void OnDisconnected()
        {
            Debug.Log("Disconnected.");
        }

        /// <summary>
        /// Override this method to take some actions when the connection is closed.
        /// </summary>
        protected virtual void OnConnectionLost()
        {
            Debug.LogWarning("CONNECTION LOST!");
        }

        /// <summary>
        /// Processing of income messages and events is postponed here in the main thread.
        /// Remember to call ProcessMqttEvents() in Update() method if you override it.
        /// </summary>
        protected virtual void Update()
        {
            ProcessMqttEvents();
        }

        protected virtual void ProcessMqttEvents()
        {
            // process messages in the main queue
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();
            // process messages income in the meanwhile
            SwapMqttMessageQueues();
            ProcessMqttMessageBackgroundQueue();

            if (mqttClientConnectionClosed)
            {
                mqttClientConnectionClosed = false;
                OnConnectionLost();
            }
        }

        private void ProcessMqttMessageBackgroundQueue()
        {
            foreach (MqttMsgPublishEventArgs msg in backMessageQueue)
            {
                DecodeMessage(msg.Topic, msg.Message);
            }
            backMessageQueue.Clear();
        }

        /// <summary>
        /// Swap the message queues to continue receiving message when processing a queue.
        /// </summary>
        private void SwapMqttMessageQueues()
        {
            frontMessageQueue = frontMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
            backMessageQueue = backMessageQueue == messageQueue1 ? messageQueue2 : messageQueue1;
        }

        private void OnMqttMessageReceived(object sender, MqttMsgPublishEventArgs msg)
        {
            frontMessageQueue.Add(msg);
        }

        private void OnMqttConnectionClosed(object sender, EventArgs e)
        {
            // Set unexpected connection closed only if connected (avoid event handling in case of controlled disconnection)
            mqttClientConnectionClosed = mqttClientConnected;
            mqttClientConnected = false;
        }

        /// <summary>
        /// Connects to the broker using the current settings.
        /// </summary>
        /// <returns>The execution is done in a coroutine.</returns>
        private IEnumerator DoConnect()
        {
            // wait for the given delay
            yield return new WaitForSecondsRealtime(connectionSettings.connectionDelay / 1000f);
            // leave some time to Unity to refresh the UI
            yield return new WaitForEndOfFrame();

            // create client instance 
            if (client == null)
            {
                try
                {
#if (!UNITY_EDITOR && UNITY_WSA_10_0 && !ENABLE_IL2CPP)
                    client = new MqttClient(connectionSettings.brokerAddress,connectionSettings.brokerPort,connectionSettings.isEncrypted, connectionSettings.isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
#else
                    client = new MqttClient(connectionSettings.brokerAddress, connectionSettings.brokerPort, connectionSettings.isEncrypted, null, null, connectionSettings.isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                    //System.Security.Cryptography.X509Certificates.X509Certificate cert = new System.Security.Cryptography.X509Certificates.X509Certificate();
                    //client = new MqttClient(brokerAddress, brokerPort, isEncrypted, cert, null, MqttSslProtocols.TLSv1_0, MyRemoteCertificateValidationCallback);
#endif
                }
                catch (Exception e)
                {
                    client = null;
                    Debug.LogErrorFormat("CONNECTION FAILED! {0}", e.ToString());
                    OnConnectionFailed(e.Message);
                    yield break;
                }
            }
            else if (client.IsConnected)
            {
                yield break;
            }
            OnConnecting();

            // leave some time to Unity to refresh the UI
            yield return new WaitForEndOfFrame();
            yield return new WaitForEndOfFrame();

            client.Settings.TimeoutOnConnection = connectionSettings.timeoutOnConnection;
            string clientId = Guid.NewGuid().ToString();
            try
            {
                client.Connect(clientId, connectionSettings.userName, connectionSettings.password);
            }
            catch (Exception e)
            {
                client = null;
                Debug.LogErrorFormat("Failed to connect to {0}:{1}:\n{2}", connectionSettings.brokerAddress, connectionSettings.brokerPort, e.ToString());
                OnConnectionFailed(e.Message);
                yield break;
            }
            if (client.IsConnected)
            {
                client.ConnectionClosed += OnMqttConnectionClosed;
                // register to message received 
                client.MqttMsgPublishReceived += OnMqttMessageReceived;
                mqttClientConnected = true;
                OnConnected();
            }
            else
            {
                OnConnectionFailed("CONNECTION FAILED!");
            }
        }

        private IEnumerator DoDisconnect()
        {
            yield return new WaitForEndOfFrame();
            CloseConnection();
            OnDisconnected();
        }

        private void CloseConnection()
        {
            mqttClientConnected = false;
            if (client != null)
            {
                if (client.IsConnected)
                {
                    UnsubscribeTopics();
                    client.Disconnect();
                }
                client.MqttMsgPublishReceived -= OnMqttMessageReceived;
                client.ConnectionClosed -= OnMqttConnectionClosed;
                client = null;
            }
        }

#if ((!UNITY_EDITOR && UNITY_WSA_10_0))
        private void OnApplicationFocus(bool focus)
        {
            // On UWP 10 (HoloLens) we cannot tell whether the application actually got closed or just minimized.
            // (https://forum.unity.com/threads/onapplicationquit-and-ondestroy-are-not-called-on-uwp-10.462597/)
            if (focus)
            {
                Connect();
            }
            else
            {
                CloseConnection();
            }
        }
#endif
    }
}