﻿using Microsoft.JSInterop;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace WebBluetooth
{
    public class BluetoothNavigator
    {
        private IJSRuntime jsRuntime;
        private readonly DotNetObjectReference<BluetoothNavigator> _dotNetRef;
        private bool initialized = false;
        public event Action<string> OnDeviceDisconnected;

        public BluetoothNavigator(IJSRuntime jsRuntime)
        {
            this.jsRuntime = jsRuntime;
            this._dotNetRef = DotNetObjectReference.Create(this);
        }

        public async Task setDotNetRefAsync() {
            if (initialized) return;
            await jsRuntime.InvokeVoidAsync("blazmwebbluetooth.setDotNetRef", _dotNetRef);
            initialized = true;
        }

        public async Task<Device> RequestDeviceAsync(RequestDeviceQuery query)
        {   
            await setDotNetRefAsync();
            string json=JsonConvert.SerializeObject(query,
                            Newtonsoft.Json.Formatting.None,
                            new JsonSerializerSettings
                            {
                                NullValueHandling = NullValueHandling.Ignore
                            });

            return await jsRuntime.InvokeAsync<Device>("blazmwebbluetooth.requestDevice",json);
        }

#region WriteValueAsync
        public async Task WriteValueAsync(string deviceId, Guid serviceId, Guid characteristicId, byte[] value)
        {
                await WriteValueAsync(deviceId, serviceId.ToString(), characteristicId.ToString(),value);
        }

        public async Task WriteValueAsync(string deviceId, Guid serviceId, string characteristicId, byte[] value)
        {
            await WriteValueAsync(deviceId, serviceId.ToString(), characteristicId, value);
        }

        public async Task WriteValueAsync(string deviceId, string serviceId, Guid characteristicId, byte[] value)
        {
            await WriteValueAsync(deviceId, serviceId, characteristicId.ToString(), value);
        }

        public async Task WriteValueAsync(string deviceId, string serviceId, string characteristicId,  byte[] value)
        {
            var bytes = value.Select(v => (uint)v).ToArray();
            await jsRuntime.InvokeVoidAsync("blazmwebbluetooth.writeValue", deviceId, serviceId.ToLowerInvariant(), characteristicId.ToLowerInvariant(), bytes);
        }
        #endregion
#region ReadValueAsync
        public async Task<byte[]> ReadValueAsync(string deviceId, string serviceId, string characteristicId)
        {
            var value= await jsRuntime.InvokeAsync<uint[]>("blazmwebbluetooth.readValue", deviceId, serviceId.ToLowerInvariant(), characteristicId.ToLowerInvariant());
            return value.Select(v => (byte)(v & 0xFF)).ToArray();
        }

        public async Task<byte[]> ReadValueAsync(string deviceId, Guid serviceId, string characteristicId)
        {
            return await ReadValueAsync(deviceId, serviceId.ToString(), characteristicId);
        }

        public async Task<byte[]> ReadValueAsync(string deviceId, string serviceId, Guid characteristicId)
        {
            return await ReadValueAsync(deviceId, serviceId, characteristicId.ToString());
        }

        public async Task<byte[]> ReadValueAsync(string deviceId, Guid serviceId, Guid characteristicId)
        {
            return await ReadValueAsync(deviceId, serviceId.ToString(), characteristicId.ToString());
        }
        #endregion

        #region SetupNotifyAsync
        private DotNetObjectReference<NotificationHandler> NotificationHandler;
        public async Task SetupNotifyAsync(string deviceId, string serviceId, string characteristicId)
        {
            if (NotificationHandler == null)
            {
                NotificationHandler = DotNetObjectReference.Create(new NotificationHandler(this));
            }

            await jsRuntime.InvokeVoidAsync("blazmwebbluetooth.setupNotify", deviceId, serviceId.ToLowerInvariant(), characteristicId.ToLowerInvariant(), NotificationHandler);
        }
        public async Task SetupNotifyAsync(string deviceId, Guid serviceId, string characteristicId)
        {
            await SetupNotifyAsync(deviceId, serviceId.ToString(), characteristicId);
        }
        public async Task SetupNotifyAsync(string deviceId, string serviceId, Guid characteristicId)
        {
            await SetupNotifyAsync(deviceId, serviceId, characteristicId.ToString());
        }
        public async Task SetupNotifyAsync(string deviceId, Guid serviceId, Guid characteristicId)
        {
            await SetupNotifyAsync(deviceId, serviceId.ToString(), characteristicId.ToString());
        }
        #endregion

        public async Task DisconnectAsync(string deviceId) {
            await jsRuntime.InvokeVoidAsync("blazmwebbluetooth.disconnectDevice", deviceId);
        }

        [JSInvokable("onBleDisconnect")]
        public void JsOnDisconnect(string deviceId) {
                OnDeviceDisconnected?.Invoke(deviceId);
        }

        public void TriggerNotification(CharacteristicEventArgs args)
        {
            Notification?.Invoke(this, args);
        }
        public event EventHandler<CharacteristicEventArgs> Notification;
    }


    public class NotificationHandler
    {

        BluetoothNavigator bluetoothNavigator;
        public NotificationHandler(BluetoothNavigator bluetoothNavigator)
        {
            this.bluetoothNavigator = bluetoothNavigator;
        }

        [JSInvokable]
        public void HandleCharacteristicValueChanged(Guid serviceGuid, Guid characteristicGuid,uint[] value)
        {
            byte[] byteArray = value.Select(u => (byte)(u & 0xff)).ToArray();

            CharacteristicEventArgs args = new CharacteristicEventArgs();
            args.ServiceId = serviceGuid;
            args.CharacteristicId = characteristicGuid;
            args.Value = byteArray;
            bluetoothNavigator.TriggerNotification(args);
        }
    }

    public class CharacteristicEventArgs : EventArgs
    {
        public Guid ServiceId { get; set; }
        public Guid CharacteristicId { get; set; }
        public byte[] Value { get; set; }
    }
}
