![R2MV](https://i.imgur.com/SAdxi7s.png)

Episode 10 - Integrate cloud-based IoT data into your XR experience 
====

![Header Image](https://i.imgur.com/g3n1m3V.png)

## About this project
Part of the [Road to Metaverse, Creator Series](https://create.unity.com/road-to-metaverse), this demo was used in Episode 10 to learn how to build a simple IoT console and dashboard application using MQTT, and Visual Scripting!

## Synopsis
Through either hardware devices, or a console application, a user is able to turn lights on or off and control the temperature in the various rooms of an office.

The state of the system is reflected through a 2D UI and in a digital twin view of the physical office.

---

## Demo
There are two scenes to explore under **R2mv10Assets/Scenes/**

### R2mvMqtt_Console 

![Console screenshot](https://i.imgur.com/cPRXTsl.png)

- Listen to UI and Midi inputs
- Sends messages to broker
- Listen to messages from broker & updates UI + digital twin

### R2mvMqtt_Dashboard

![Console screenshot](https://i.imgur.com/jm45zVH.png)

- Listen to messages from broker
- Updates UI and 3D Scene values
- No user inputs

<br>

![Editor screenshot](https://i.imgur.com/v8uGKCg.png)

### Working with M2MQTT

- M2MQTT for Unity adds a **M2MqttUnityClient** MonoBehaviour that wraps the M2QTT .NET client
- Created a new manager class **MqttBrokerConnectionManager** to expose more events than M2MqttUnityClient, and implement a Scriptable Object **MqttBrokerConnectionSettings** to hold broker settings. Using a Scriptable Object makes switching between brokers quick and easy
- **MqttBrokerConnectionSettings** also holds a list of Topics to subscribe to
    - R2mvDemo/cc1 - 8 (continuous change) handle knobs values from 0 to 1
    - R2mvDemo/t1 - 8 (toggle) handle pads / taps toggle as True or False
- **MqttBrokerVSNotificationRelay** connects to the connection manager and handles sending or receiving Visual Scripting Notifications
    - Holds 4 Notifications
        -  Message Decoded (Incoming MQTT message)
        - Publish (Send message to MQTT Broker)
        - Connection Succeeded
        - Connection Failed
- **Notifications** are custom Visual Scripting units 
    - handle or send messages through the Event Bus
    - Use a scriptable object to define notifications types
    - Can include arguments (variable / values)


---

The project uses the following resources:
- Fork of [M2MQTT for Unity](https://github.com/gpvigano/M2MqttUnity) by [Giovanni Paolo Viganò](https://github.com/gpvigano)
- [MinisVS](https://github.com/keijiro/MinisVS) for midi support
- [Notifications for Visual Scripting](https://github.com/RoadToTheMetaverse/visualscripting-notifications)


Need more info, or have some questions? Head over to our [forums](https://forum.unity.com/threads/workshops-integrate-cloud-based-iot-data-into-your-xr-experience.1293402/).