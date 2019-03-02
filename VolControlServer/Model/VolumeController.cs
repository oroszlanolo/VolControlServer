using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using Interops;

namespace VolControlServer.Model
{
    public class VolumeController
    {
        private List<string> names;
        private List<ISimpleAudioVolume> controls;

        public VolumeController()
        {
            names = new List<string>();
            controls = new List<ISimpleAudioVolume>();
        }

        public int Count()
        {
            return names.Count;
        }
        public string GetName(int index)
        {
            if (index >= 0 && index < names.Count)
                return names[index];
            else
                return "Invalid index";
        }
        public float GetVolume(int index)
        {
            if (index >= 0 && index < names.Count)
            {
                controls[index].GetMasterVolume(out float currvol);
                return currvol;
            }
            else
                return -1;
        }
        public void SetVolume(int index, float val)
        {
            if(index >= 0 && index < controls.Count)
            {
                Guid g = Guid.Empty;
                controls[index].SetMasterVolume(val, ref g);
            }
        }
        public void SetVolume(string name, float val)
        {
            int index = names.IndexOf(name);
            if (index == -1)
                return;

            Guid g = Guid.Empty;
            controls[index].SetMasterVolume(val, ref g);
        }

        public void Refresh()
        {
            ResetLists();

            MMDeviceEnumerator dev_enumerator = new MMDeviceEnumerator();
            ((IMMDeviceEnumerator)dev_enumerator).GetDefaultAudioEndpoint(EDataFlow.eRender, ERole.eMultimedia, out IMMDevice dev);
            Marshal.ReleaseComObject(dev_enumerator);

            if (dev == null)
                return;

            Guid IID_IAudioSessionManager2 = typeof(IAudioSessionManager2).GUID;
            dev.Activate(ref IID_IAudioSessionManager2, 0, IntPtr.Zero, out object result_obj);
            IAudioSessionManager2 mgr = (IAudioSessionManager2)result_obj;
            Marshal.ReleaseComObject(dev);

            if (mgr == null)
                return;

            mgr.GetSessionEnumerator(out IAudioSessionEnumerator sessionEnumerator);
            Marshal.ReleaseComObject(mgr);

            if (sessionEnumerator == null)
                return;

            RefreshInner(sessionEnumerator);
            
        }

        private void RefreshInner(IAudioSessionEnumerator sessionEnumerator)
        {
            sessionEnumerator.GetCount(out int count);

            for (int i = 0; i < count; i++)
            {
                ISimpleAudioVolume volumeControl = null;
                sessionEnumerator.GetSession(i, out IAudioSessionControl ctl);
                ctl.GetState(out AudioSessionState state);
                ((IAudioSessionControl2)ctl).GetProcessId(out int procid);
                Process currproc = null;
                try
                {
                    currproc = Process.GetProcessById(procid);
                }
                catch (ArgumentException) { }

                if (currproc != null && (state == AudioSessionState.AudioSessionStateActive || state == AudioSessionState.AudioSessionStateInactive))
                {
                    ctl.GetDisplayName(out string dn);
                    dn = dn + "\t" + currproc.ProcessName;
                    volumeControl = (ISimpleAudioVolume)ctl;
                    names.Add(dn);
                    controls.Add(volumeControl);
                }
            }
        }

        private void ResetLists()
        {

            foreach (ISimpleAudioVolume vct in controls)
                Marshal.ReleaseComObject(vct);

            names = new List<string>();
            controls = new List<ISimpleAudioVolume>();
        }
    }

}
