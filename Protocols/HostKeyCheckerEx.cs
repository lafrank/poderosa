﻿// Copyright 2004-2017 The Poderosa Project.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.Text;

using Granados;
using Granados.KnownHosts;

namespace Poderosa.Protocols {
    /// <summary>
    /// 
    /// </summary>
    /// <exclude/>
    public interface ISSHHostKeyVerifier2 {
        bool Verify(ISSHHostKeyInformationProvider info);
    }

    //Extension Point
    public class HostKeyVerifierBridge {
        private ISSHHostKeyVerifier2 _verifier;

        public bool Vefiry(ISSHHostKeyInformationProvider info) {
            if (_verifier == null)
                _verifier = FindHostKeyVerifier();
            if (_verifier == null)
                return true; //普通KnownHostsくらいはあるだろう。エラーにすべきかもしれないが
            else
                return _verifier.Verify(info);
        }

        private ISSHHostKeyVerifier2 FindHostKeyVerifier() {
            ISSHHostKeyVerifier2[] vs = (ISSHHostKeyVerifier2[])ProtocolsPlugin.Instance.PoderosaWorld.PluginManager.FindExtensionPoint(ProtocolsPluginConstants.HOSTKEYCHECKER_EXTENSION).GetExtensions();
            string name = PEnv.Options.HostKeyCheckerVerifierTypeName; //一応隠しpreference

            //何か入っていたら登録
            if (name.Length > 0) {
                foreach (ISSHHostKeyVerifier2 v in vs) {
                    if (v.GetType().FullName == name)
                        return v;
                }
            }
            return null;
        }
    }
}
