using System;
using Notifications;
using Unity.VisualScripting;
using UnityEngine;

namespace R2mv.Mqtt
{
    /// <summary>
    /// This component connects the MqttBrokerConnectionManager component to the Visual Scripting Notification system.
    /// It handles connection success and failure, send and receive MQTT messages.  
    /// </summary>
    [RequireComponent(typeof(MqttBrokerConnectionManager))]
    public class MqttBrokerVSNotificationRelay : MonoBehaviour
    {

        [Header("Notifications")]
        [Tooltip("Used to dispatch when a new MQTT message was received.")]
        public NotificationTypeScriptableObject messageDecodedNotification;
        
        [Tooltip("Used to send a new MQTT message through the Broker Manager.")] 
        public NotificationTypeScriptableObject publishNotification;
        
        [Tooltip("Dispatched when the application successfully connects to the broker.")]
        public NotificationTypeScriptableObject connectionSucceededNotification;
        
        [Tooltip("Dispatched when the connection to the broker failed.")]
        public NotificationTypeScriptableObject connectionFailedNotification;

        private MqttBrokerConnectionManager _connectionManager;
        
        private Action<CustomEventArgs> OnNewNotification { get; set; }
        
        private void Awake()
        {
            _connectionManager = GetComponent<MqttBrokerConnectionManager>();
            OnNewNotification = args => ProcessNewNotification(args);
        }

        private void OnEnable()
        {
            EventBus.Register<CustomEventArgs>(NotificationEventUnit.EventBusHook, OnNewNotification);
            _connectionManager.MessageDecoded += OnMessageDecoded;
            _connectionManager.ConnectionSucceeded += OnConnectionSucceeded;
            _connectionManager.ConnectionFailed += OnConnectionFailed;
        }


        private void OnDisable()
        {
            EventBus.Unregister(NotificationEventUnit.EventBusHook, OnNewNotification);
            _connectionManager.MessageDecoded -= OnMessageDecoded;
            _connectionManager.ConnectionSucceeded -= OnConnectionSucceeded;
            _connectionManager.ConnectionFailed -= OnConnectionFailed;
        }

        private void ProcessNewNotification(CustomEventArgs args)
        {
            if (!publishNotification)
            {
                Debug.LogError("The publish notification is not defined. Create and/or attach a notification asset.");
                return;
            }
            
            if (args.name == publishNotification.NotificationUniqueID)
            {
                var topic = args.arguments[0].ToString();
                var message = args.arguments[1].ToString();
                
                _connectionManager.Publish(topic, message);
            }
        }
        
        private void OnMessageDecoded(string topic, string message)
        {
            if (!messageDecodedNotification)
            {
                Debug.LogError("The message decoded notification is not defined. Create and/or attach a notification asset.");
                return;
            }
            
            var name = messageDecodedNotification.NotificationUniqueID;
            object[] arguments = {topic,  message};
            
            EventBus.Trigger(NotificationEventUnit.EventBusHook,  new CustomEventArgs(name, arguments) );
        }

        private void OnConnectionFailed()
        {
            if (!connectionFailedNotification)
            {
                Debug.LogError("The connection failed notification is not defined. Create and/or attach a notification asset.");
                return;
            }
            
            var name = connectionFailedNotification.NotificationUniqueID;
            EventBus.Trigger(NotificationEventUnit.EventBusHook,  new CustomEventArgs(name) );
        }

        private void OnConnectionSucceeded()
        {
            if (!connectionSucceededNotification)
            {
                Debug.LogError("The connection succeeded notification is not defined. Create and/or attach a notification asset.");
                return;
            }
            
            var name = connectionSucceededNotification.NotificationUniqueID;
            EventBus.Trigger(NotificationEventUnit.EventBusHook,  new CustomEventArgs(name) );
        }
        
    }
}