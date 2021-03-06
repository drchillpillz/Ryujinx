﻿using Ryujinx.Common.Logging;
using Ryujinx.HLE.HOS.Applets;
using Ryujinx.HLE.HOS.Ipc;
using Ryujinx.HLE.HOS.Kernel.Common;
using Ryujinx.HLE.HOS.Kernel.Threading;
using System;

namespace Ryujinx.HLE.HOS.Services.Am.AppletAE.AllSystemAppletProxiesService.LibraryAppletCreator
{
    class ILibraryAppletAccessor : IpcService
    {
        private IApplet _applet;

        private AppletSession _normalSession;
        private AppletSession _interactiveSession;

        private KEvent _stateChangedEvent;
        private KEvent _normalOutDataEvent;
        private KEvent _interactiveOutDataEvent;

        public ILibraryAppletAccessor(AppletId appletId, Horizon system)
        {
            _stateChangedEvent       = new KEvent(system.KernelContext);
            _normalOutDataEvent      = new KEvent(system.KernelContext);
            _interactiveOutDataEvent = new KEvent(system.KernelContext);

            _applet = AppletManager.Create(appletId, system);

            _normalSession      = new AppletSession();
            _interactiveSession = new AppletSession();

            _applet.AppletStateChanged        += OnAppletStateChanged;
            _normalSession.DataAvailable      += OnNormalOutData;
            _interactiveSession.DataAvailable += OnInteractiveOutData;
            
            Logger.PrintInfo(LogClass.ServiceAm, $"Applet '{appletId}' created.");
        }

        private void OnAppletStateChanged(object sender, EventArgs e)
        {
            _stateChangedEvent.WritableEvent.Signal();
        }

        private void OnNormalOutData(object sender, EventArgs e)
        {
            _normalOutDataEvent.WritableEvent.Signal();
        }

        private void OnInteractiveOutData(object sender, EventArgs e)
        {
            _interactiveOutDataEvent.WritableEvent.Signal();
        }

        [Command(0)]
        // GetAppletStateChangedEvent() -> handle<copy>
        public ResultCode GetAppletStateChangedEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_stateChangedEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            return ResultCode.Success;
        }

        [Command(10)]
        // Start()
        public ResultCode Start(ServiceCtx context)
        {
            return (ResultCode)_applet.Start(_normalSession.GetConsumer(),
                                             _interactiveSession.GetConsumer());
        }

        [Command(30)]
        // GetResult()
        public ResultCode GetResult(ServiceCtx context)
        {
            return (ResultCode)_applet.GetResult();
        }

        [Command(100)]
        // PushInData(object<nn::am::service::IStorage>)
        public ResultCode PushInData(ServiceCtx context)
        {
            IStorage data = GetObject<IStorage>(context, 0);

            _normalSession.Push(data.Data);

            return ResultCode.Success;
        }

        [Command(101)]
        // PopOutData() -> object<nn::am::service::IStorage>
        public ResultCode PopOutData(ServiceCtx context)
        {
            if(_normalSession.TryPop(out byte[] data))
            {
                MakeObject(context, new IStorage(data));

                _normalOutDataEvent.WritableEvent.Clear();

                return ResultCode.Success;
            }

            return ResultCode.NotAvailable;
        }

        [Command(103)]
        // PushInteractiveInData(object<nn::am::service::IStorage>)
        public ResultCode PushInteractiveInData(ServiceCtx context)
        {
            IStorage data = GetObject<IStorage>(context, 0);

            _interactiveSession.Push(data.Data);

            return ResultCode.Success;
        }

        [Command(104)]
        // PopInteractiveOutData() -> object<nn::am::service::IStorage>
        public ResultCode PopInteractiveOutData(ServiceCtx context)
        {
            if(_interactiveSession.TryPop(out byte[] data))
            {
                MakeObject(context, new IStorage(data));

                _interactiveOutDataEvent.WritableEvent.Clear();

                return ResultCode.Success;
            }

            return ResultCode.NotAvailable;
        }

        [Command(105)]
        // GetPopOutDataEvent() -> handle<copy>
        public ResultCode GetPopOutDataEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_normalOutDataEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            return ResultCode.Success;
        }

        [Command(106)]
        // GetPopInteractiveOutDataEvent() -> handle<copy>
        public ResultCode GetPopInteractiveOutDataEvent(ServiceCtx context)
        {
            if (context.Process.HandleTable.GenerateHandle(_interactiveOutDataEvent.ReadableEvent, out int handle) != KernelResult.Success)
            {
                throw new InvalidOperationException("Out of handles!");
            }

            context.Response.HandleDesc = IpcHandleDesc.MakeCopy(handle);

            return ResultCode.Success;
        }
    }
}
