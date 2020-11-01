﻿// Copyright(c) 2019 pypy. All rights reserved.
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using DiscordRPC;
using System.Text;
using System.Threading;

namespace VRCX
{
    public class Discord
    {
        public static Discord Instance { get; private set; }
        private readonly ReaderWriterLockSlim m_Lock;
        private readonly RichPresence m_Presence;
        private DiscordRpcClient m_Client;
        private Thread m_Thread;
        private bool m_Active;

        static Discord()
        {
            Instance = new Discord();
        }

        public Discord()
        {
            m_Lock = new ReaderWriterLockSlim();
            m_Presence = new RichPresence();
            m_Thread = new Thread(ThreadLoop)
            {
                IsBackground = true
            };
        }

        internal void Init()
        {
            m_Thread.Start();
        }

        internal void Exit()
        {
            var thread = m_Thread;
            m_Thread = null;
            thread.Interrupt();
            thread.Join();

            m_Client?.Dispose();
        }

        private void ThreadLoop()
        {
            while (m_Thread != null)
            {
                Update();

                try
                {
                    Thread.Sleep(1000);
                }
                catch
                {
                    // ThreadInterruptedException
                }
            }
        }

        private void Update()
        {
            if (m_Client != null)
            {
                m_Lock.EnterReadLock();
                try
                {
                    m_Client.SetPresence(m_Presence);
                }
                finally
                {
                    m_Lock.ExitReadLock();
                }
                m_Client.Invoke();
            }

            if (m_Active == true)
            {
                if (m_Client == null)
                {
                    m_Client = new DiscordRpcClient("525953831020920832");
                    if (m_Client.Initialize() == false)
                    {
                        m_Client.Dispose();
                        m_Client = null;
                    }
                }
            }
            else if (m_Client != null)
            {
                m_Client.Dispose();
                m_Client = null;
            }
        }

        public void SetActive(bool active)
        {
            m_Active = active;
        }

        // https://stackoverflow.com/questions/1225052/best-way-to-shorten-utf8-string-based-on-byte-length
        private static string LimitByteLength(string str, int maxBytesLength)
        {
            var bytesArr = Encoding.UTF8.GetBytes(str);
            var bytesToRemove = 0;
            var lastIndexInString = str.Length - 1;
            while (bytesArr.Length - bytesToRemove > maxBytesLength)
            {
                bytesToRemove += Encoding.UTF8.GetByteCount(new char[] { str[lastIndexInString] });
                --lastIndexInString;
            }
            return Encoding.UTF8.GetString(bytesArr, 0, bytesArr.Length - bytesToRemove);
        }

        public void SetText(string details, string state)
        {
            m_Lock.EnterWriteLock();
            try
            {
                m_Presence.Details = LimitByteLength(details, 127);
                m_Presence.State = LimitByteLength(state, 127);
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        public void SetAssets(string largeKey, string largeText, string smallKey, string smallText)
        {
            m_Lock.EnterWriteLock();
            try
            {
                if (string.IsNullOrEmpty(largeKey) == true &&
                    string.IsNullOrEmpty(smallKey) == true)
                {
                    m_Presence.Assets = null;
                }
                else
                {
                    if (m_Presence.Assets == null)
                    {
                        m_Presence.Assets = new Assets();
                    }
                    m_Presence.Assets.LargeImageKey = largeKey;
                    m_Presence.Assets.LargeImageText = largeText;
                    m_Presence.Assets.SmallImageKey = smallKey;
                    m_Presence.Assets.SmallImageText = smallText;
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }

        public void SetTimestamps(double startUnixMilliseconds, double endUnixMilliseconds)
        {
            var _startUnixMilliseconds = (ulong)startUnixMilliseconds;
            var _endUnixMilliseconds = (ulong)endUnixMilliseconds;

            m_Lock.EnterWriteLock();
            try
            {
                if (_startUnixMilliseconds == 0)
                {
                    m_Presence.Timestamps = null;
                }
                else
                {
                    if (m_Presence.Timestamps == null)
                    {
                        m_Presence.Timestamps = new Timestamps();
                    }

                    m_Presence.Timestamps.StartUnixMilliseconds = _startUnixMilliseconds;

                    if (_endUnixMilliseconds == 0)
                    {
                        m_Presence.Timestamps.End = null;
                    }
                    else
                    {
                        m_Presence.Timestamps.EndUnixMilliseconds = _endUnixMilliseconds;
                    }
                }
            }
            finally
            {
                m_Lock.ExitWriteLock();
            }
        }
    }
}