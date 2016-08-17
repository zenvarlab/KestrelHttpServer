
using System;
using System.Collections.Generic;

namespace Microsoft.AspNetCore.Server.Kestrel.Internal.Http
{
    public partial class Frame
    {
        private static readonly Type IHttpRequestFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpRequestFeature);
        private static readonly Type IHttpResponseFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpResponseFeature);
        private static readonly Type IHttpRequestIdentifierFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature);
        private static readonly Type IServiceProvidersFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IServiceProvidersFeature);
        private static readonly Type IHttpRequestLifetimeFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature);
        private static readonly Type IHttpConnectionFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature);
        private static readonly Type IHttpAuthenticationFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature);
        private static readonly Type IQueryFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IQueryFeature);
        private static readonly Type IFormFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IFormFeature);
        private static readonly Type IHttpUpgradeFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature);
        private static readonly Type IResponseCookiesFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IResponseCookiesFeature);
        private static readonly Type IItemsFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IItemsFeature);
        private static readonly Type ITlsConnectionFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature);
        private static readonly Type IHttpWebSocketFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature);
        private static readonly Type ISessionFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.ISessionFeature);
        private static readonly Type IHttpSendFileFeatureType = typeof(global::Microsoft.AspNetCore.Http.Features.IHttpSendFileFeature);

        private object _currentIHttpRequestFeature;
        private object _stateIHttpRequestFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpRequestFeature> _factoryIHttpRequestFeature;

        private object _currentIHttpResponseFeature;
        private object _stateIHttpResponseFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpResponseFeature> _factoryIHttpResponseFeature;

        private object _currentIHttpRequestIdentifierFeature;
        private object _stateIHttpRequestIdentifierFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature> _factoryIHttpRequestIdentifierFeature;

        private object _currentIServiceProvidersFeature;
        private object _stateIServiceProvidersFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IServiceProvidersFeature> _factoryIServiceProvidersFeature;

        private object _currentIHttpRequestLifetimeFeature;
        private object _stateIHttpRequestLifetimeFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature> _factoryIHttpRequestLifetimeFeature;

        private object _currentIHttpConnectionFeature;
        private object _stateIHttpConnectionFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature> _factoryIHttpConnectionFeature;

        private object _currentIHttpAuthenticationFeature;
        private object _stateIHttpAuthenticationFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature> _factoryIHttpAuthenticationFeature;

        private object _currentIQueryFeature;
        private object _stateIQueryFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IQueryFeature> _factoryIQueryFeature;

        private object _currentIFormFeature;
        private object _stateIFormFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IFormFeature> _factoryIFormFeature;

        private object _currentIHttpUpgradeFeature;
        private object _stateIHttpUpgradeFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature> _factoryIHttpUpgradeFeature;

        private object _currentIResponseCookiesFeature;
        private object _stateIResponseCookiesFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IResponseCookiesFeature> _factoryIResponseCookiesFeature;

        private object _currentIItemsFeature;
        private object _stateIItemsFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IItemsFeature> _factoryIItemsFeature;

        private object _currentITlsConnectionFeature;
        private object _stateITlsConnectionFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature> _factoryITlsConnectionFeature;

        private object _currentIHttpWebSocketFeature;
        private object _stateIHttpWebSocketFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature> _factoryIHttpWebSocketFeature;

        private object _currentISessionFeature;
        private object _stateISessionFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.ISessionFeature> _factoryISessionFeature;

        private object _currentIHttpSendFileFeature;
        private object _stateIHttpSendFileFeature;
        private Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpSendFileFeature> _factoryIHttpSendFileFeature;


        private void FastReset()
        {
            _currentIHttpRequestFeature = this;
            _currentIHttpResponseFeature = this;
            _currentIHttpUpgradeFeature = this;
            _currentIHttpRequestLifetimeFeature = this;
            _currentIHttpConnectionFeature = this;
            
            _currentIHttpRequestIdentifierFeature = null;
            _currentIServiceProvidersFeature = null;
            _currentIHttpAuthenticationFeature = null;
            _currentIQueryFeature = null;
            _currentIFormFeature = null;
            _currentIResponseCookiesFeature = null;
            _currentIItemsFeature = null;
            _currentITlsConnectionFeature = null;
            _currentIHttpWebSocketFeature = null;
            _currentISessionFeature = null;
            _currentIHttpSendFileFeature = null;
        }

        private object FastFeatureGet(Type key)
        {
            if (key == IHttpRequestFeatureType)
            {
                if (_currentIHttpRequestFeature == null && _factoryIHttpRequestFeature != null)
                {
                    _currentIHttpRequestFeature = _factoryIHttpRequestFeature(_stateIHttpRequestFeature);
                }
                return _currentIHttpRequestFeature;
            }
            if (key == IHttpResponseFeatureType)
            {
                if (_currentIHttpResponseFeature == null && _factoryIHttpResponseFeature != null)
                {
                    _currentIHttpResponseFeature = _factoryIHttpResponseFeature(_stateIHttpResponseFeature);
                }
                return _currentIHttpResponseFeature;
            }
            if (key == IHttpRequestIdentifierFeatureType)
            {
                if (_currentIHttpRequestIdentifierFeature == null && _factoryIHttpRequestIdentifierFeature != null)
                {
                    _currentIHttpRequestIdentifierFeature = _factoryIHttpRequestIdentifierFeature(_stateIHttpRequestIdentifierFeature);
                }
                return _currentIHttpRequestIdentifierFeature;
            }
            if (key == IServiceProvidersFeatureType)
            {
                if (_currentIServiceProvidersFeature == null && _factoryIServiceProvidersFeature != null)
                {
                    _currentIServiceProvidersFeature = _factoryIServiceProvidersFeature(_stateIServiceProvidersFeature);
                }
                return _currentIServiceProvidersFeature;
            }
            if (key == IHttpRequestLifetimeFeatureType)
            {
                if (_currentIHttpRequestLifetimeFeature == null && _factoryIHttpRequestLifetimeFeature != null)
                {
                    _currentIHttpRequestLifetimeFeature = _factoryIHttpRequestLifetimeFeature(_stateIHttpRequestLifetimeFeature);
                }
                return _currentIHttpRequestLifetimeFeature;
            }
            if (key == IHttpConnectionFeatureType)
            {
                if (_currentIHttpConnectionFeature == null && _factoryIHttpConnectionFeature != null)
                {
                    _currentIHttpConnectionFeature = _factoryIHttpConnectionFeature(_stateIHttpConnectionFeature);
                }
                return _currentIHttpConnectionFeature;
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                if (_currentIHttpAuthenticationFeature == null && _factoryIHttpAuthenticationFeature != null)
                {
                    _currentIHttpAuthenticationFeature = _factoryIHttpAuthenticationFeature(_stateIHttpAuthenticationFeature);
                }
                return _currentIHttpAuthenticationFeature;
            }
            if (key == IQueryFeatureType)
            {
                if (_currentIQueryFeature == null && _factoryIQueryFeature != null)
                {
                    _currentIQueryFeature = _factoryIQueryFeature(_stateIQueryFeature);
                }
                return _currentIQueryFeature;
            }
            if (key == IFormFeatureType)
            {
                if (_currentIFormFeature == null && _factoryIFormFeature != null)
                {
                    _currentIFormFeature = _factoryIFormFeature(_stateIFormFeature);
                }
                return _currentIFormFeature;
            }
            if (key == IHttpUpgradeFeatureType)
            {
                if (_currentIHttpUpgradeFeature == null && _factoryIHttpUpgradeFeature != null)
                {
                    _currentIHttpUpgradeFeature = _factoryIHttpUpgradeFeature(_stateIHttpUpgradeFeature);
                }
                return _currentIHttpUpgradeFeature;
            }
            if (key == IResponseCookiesFeatureType)
            {
                if (_currentIResponseCookiesFeature == null && _factoryIResponseCookiesFeature != null)
                {
                    _currentIResponseCookiesFeature = _factoryIResponseCookiesFeature(_stateIResponseCookiesFeature);
                }
                return _currentIResponseCookiesFeature;
            }
            if (key == IItemsFeatureType)
            {
                if (_currentIItemsFeature == null && _factoryIItemsFeature != null)
                {
                    _currentIItemsFeature = _factoryIItemsFeature(_stateIItemsFeature);
                }
                return _currentIItemsFeature;
            }
            if (key == ITlsConnectionFeatureType)
            {
                if (_currentITlsConnectionFeature == null && _factoryITlsConnectionFeature != null)
                {
                    _currentITlsConnectionFeature = _factoryITlsConnectionFeature(_stateITlsConnectionFeature);
                }
                return _currentITlsConnectionFeature;
            }
            if (key == IHttpWebSocketFeatureType)
            {
                if (_currentIHttpWebSocketFeature == null && _factoryIHttpWebSocketFeature != null)
                {
                    _currentIHttpWebSocketFeature = _factoryIHttpWebSocketFeature(_stateIHttpWebSocketFeature);
                }
                return _currentIHttpWebSocketFeature;
            }
            if (key == ISessionFeatureType)
            {
                if (_currentISessionFeature == null && _factoryISessionFeature != null)
                {
                    _currentISessionFeature = _factoryISessionFeature(_stateISessionFeature);
                }
                return _currentISessionFeature;
            }
            if (key == IHttpSendFileFeatureType)
            {
                if (_currentIHttpSendFileFeature == null && _factoryIHttpSendFileFeature != null)
                {
                    _currentIHttpSendFileFeature = _factoryIHttpSendFileFeature(_stateIHttpSendFileFeature);
                }
                return _currentIHttpSendFileFeature;
            }
            return ExtraFeatureGet(key);
        }

        private void FastFeatureSet(Type key, object feature)
        {
            _featureRevision++;
            
            if (key == IHttpRequestFeatureType)
            {
                _currentIHttpRequestFeature = feature;
                return;
            }
            if (key == IHttpResponseFeatureType)
            {
                _currentIHttpResponseFeature = feature;
                return;
            }
            if (key == IHttpRequestIdentifierFeatureType)
            {
                _currentIHttpRequestIdentifierFeature = feature;
                return;
            }
            if (key == IServiceProvidersFeatureType)
            {
                _currentIServiceProvidersFeature = feature;
                return;
            }
            if (key == IHttpRequestLifetimeFeatureType)
            {
                _currentIHttpRequestLifetimeFeature = feature;
                return;
            }
            if (key == IHttpConnectionFeatureType)
            {
                _currentIHttpConnectionFeature = feature;
                return;
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                _currentIHttpAuthenticationFeature = feature;
                return;
            }
            if (key == IQueryFeatureType)
            {
                _currentIQueryFeature = feature;
                return;
            }
            if (key == IFormFeatureType)
            {
                _currentIFormFeature = feature;
                return;
            }
            if (key == IHttpUpgradeFeatureType)
            {
                _currentIHttpUpgradeFeature = feature;
                return;
            }
            if (key == IResponseCookiesFeatureType)
            {
                _currentIResponseCookiesFeature = feature;
                return;
            }
            if (key == IItemsFeatureType)
            {
                _currentIItemsFeature = feature;
                return;
            }
            if (key == ITlsConnectionFeatureType)
            {
                _currentITlsConnectionFeature = feature;
                return;
            }
            if (key == IHttpWebSocketFeatureType)
            {
                _currentIHttpWebSocketFeature = feature;
                return;
            }
            if (key == ISessionFeatureType)
            {
                _currentISessionFeature = feature;
                return;
            }
            if (key == IHttpSendFileFeatureType)
            {
                _currentIHttpSendFileFeature = feature;
                return;
            };
            ExtraFeatureSet(key, feature);
        }

        private void FastFeatureFactorySet<T>(Type key, Func<object, T> feature, object state)
        {
            _featureRevision++;
            
            if (key == IHttpRequestFeatureType)
            {
                _stateIHttpRequestFeature = state;
                _factoryIHttpRequestFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpRequestFeature>;
                return;
            }
            if (key == IHttpResponseFeatureType)
            {
                _stateIHttpResponseFeature = state;
                _factoryIHttpResponseFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpResponseFeature>;
                return;
            }
            if (key == IHttpRequestIdentifierFeatureType)
            {
                _stateIHttpRequestIdentifierFeature = state;
                _factoryIHttpRequestIdentifierFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature>;
                return;
            }
            if (key == IServiceProvidersFeatureType)
            {
                _stateIServiceProvidersFeature = state;
                _factoryIServiceProvidersFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IServiceProvidersFeature>;
                return;
            }
            if (key == IHttpRequestLifetimeFeatureType)
            {
                _stateIHttpRequestLifetimeFeature = state;
                _factoryIHttpRequestLifetimeFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature>;
                return;
            }
            if (key == IHttpConnectionFeatureType)
            {
                _stateIHttpConnectionFeature = state;
                _factoryIHttpConnectionFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature>;
                return;
            }
            if (key == IHttpAuthenticationFeatureType)
            {
                _stateIHttpAuthenticationFeature = state;
                _factoryIHttpAuthenticationFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature>;
                return;
            }
            if (key == IQueryFeatureType)
            {
                _stateIQueryFeature = state;
                _factoryIQueryFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IQueryFeature>;
                return;
            }
            if (key == IFormFeatureType)
            {
                _stateIFormFeature = state;
                _factoryIFormFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IFormFeature>;
                return;
            }
            if (key == IHttpUpgradeFeatureType)
            {
                _stateIHttpUpgradeFeature = state;
                _factoryIHttpUpgradeFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature>;
                return;
            }
            if (key == IResponseCookiesFeatureType)
            {
                _stateIResponseCookiesFeature = state;
                _factoryIResponseCookiesFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IResponseCookiesFeature>;
                return;
            }
            if (key == IItemsFeatureType)
            {
                _stateIItemsFeature = state;
                _factoryIItemsFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IItemsFeature>;
                return;
            }
            if (key == ITlsConnectionFeatureType)
            {
                _stateITlsConnectionFeature = state;
                _factoryITlsConnectionFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature>;
                return;
            }
            if (key == IHttpWebSocketFeatureType)
            {
                _stateIHttpWebSocketFeature = state;
                _factoryIHttpWebSocketFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature>;
                return;
            }
            if (key == ISessionFeatureType)
            {
                _stateISessionFeature = state;
                _factoryISessionFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.ISessionFeature>;
                return;
            }
            if (key == IHttpSendFileFeatureType)
            {
                _stateIHttpSendFileFeature = state;
                _factoryIHttpSendFileFeature = feature as Func<object, global::Microsoft.AspNetCore.Http.Features.IHttpSendFileFeature>;
                return;
            };
            ExtraFeatureSet(key, feature);
        }

        private IEnumerable<KeyValuePair<Type, object>> FastEnumerable()
        {
            if (_currentIHttpRequestFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestFeatureType, (_currentIHttpRequestFeature ?? _factoryIHttpRequestFeature?.Invoke(_stateIHttpRequestFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpRequestFeature);
            }
            if (_currentIHttpResponseFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpResponseFeatureType, (_currentIHttpResponseFeature ?? _factoryIHttpResponseFeature?.Invoke(_stateIHttpResponseFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpResponseFeature);
            }
            if (_currentIHttpRequestIdentifierFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestIdentifierFeatureType, (_currentIHttpRequestIdentifierFeature ?? _factoryIHttpRequestIdentifierFeature?.Invoke(_stateIHttpRequestIdentifierFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpRequestIdentifierFeature);
            }
            if (_currentIServiceProvidersFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IServiceProvidersFeatureType, (_currentIServiceProvidersFeature ?? _factoryIServiceProvidersFeature?.Invoke(_stateIServiceProvidersFeature)) as global::Microsoft.AspNetCore.Http.Features.IServiceProvidersFeature);
            }
            if (_currentIHttpRequestLifetimeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpRequestLifetimeFeatureType, (_currentIHttpRequestLifetimeFeature ?? _factoryIHttpRequestLifetimeFeature?.Invoke(_stateIHttpRequestLifetimeFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpRequestLifetimeFeature);
            }
            if (_currentIHttpConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpConnectionFeatureType, (_currentIHttpConnectionFeature ?? _factoryIHttpConnectionFeature?.Invoke(_stateIHttpConnectionFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpConnectionFeature);
            }
            if (_currentIHttpAuthenticationFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpAuthenticationFeatureType, (_currentIHttpAuthenticationFeature ?? _factoryIHttpAuthenticationFeature?.Invoke(_stateIHttpAuthenticationFeature)) as global::Microsoft.AspNetCore.Http.Features.Authentication.IHttpAuthenticationFeature);
            }
            if (_currentIQueryFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IQueryFeatureType, (_currentIQueryFeature ?? _factoryIQueryFeature?.Invoke(_stateIQueryFeature)) as global::Microsoft.AspNetCore.Http.Features.IQueryFeature);
            }
            if (_currentIFormFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IFormFeatureType, (_currentIFormFeature ?? _factoryIFormFeature?.Invoke(_stateIFormFeature)) as global::Microsoft.AspNetCore.Http.Features.IFormFeature);
            }
            if (_currentIHttpUpgradeFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpUpgradeFeatureType, (_currentIHttpUpgradeFeature ?? _factoryIHttpUpgradeFeature?.Invoke(_stateIHttpUpgradeFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpUpgradeFeature);
            }
            if (_currentIResponseCookiesFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IResponseCookiesFeatureType, (_currentIResponseCookiesFeature ?? _factoryIResponseCookiesFeature?.Invoke(_stateIResponseCookiesFeature)) as global::Microsoft.AspNetCore.Http.Features.IResponseCookiesFeature);
            }
            if (_currentIItemsFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IItemsFeatureType, (_currentIItemsFeature ?? _factoryIItemsFeature?.Invoke(_stateIItemsFeature)) as global::Microsoft.AspNetCore.Http.Features.IItemsFeature);
            }
            if (_currentITlsConnectionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(ITlsConnectionFeatureType, (_currentITlsConnectionFeature ?? _factoryITlsConnectionFeature?.Invoke(_stateITlsConnectionFeature)) as global::Microsoft.AspNetCore.Http.Features.ITlsConnectionFeature);
            }
            if (_currentIHttpWebSocketFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpWebSocketFeatureType, (_currentIHttpWebSocketFeature ?? _factoryIHttpWebSocketFeature?.Invoke(_stateIHttpWebSocketFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpWebSocketFeature);
            }
            if (_currentISessionFeature != null)
            {
                yield return new KeyValuePair<Type, object>(ISessionFeatureType, (_currentISessionFeature ?? _factoryISessionFeature?.Invoke(_stateISessionFeature)) as global::Microsoft.AspNetCore.Http.Features.ISessionFeature);
            }
            if (_currentIHttpSendFileFeature != null)
            {
                yield return new KeyValuePair<Type, object>(IHttpSendFileFeatureType, (_currentIHttpSendFileFeature ?? _factoryIHttpSendFileFeature?.Invoke(_stateIHttpSendFileFeature)) as global::Microsoft.AspNetCore.Http.Features.IHttpSendFileFeature);
            }

            if (MaybeExtra != null)
            {
                foreach(var item in MaybeExtra)
                {
                    yield return item;
                }
            }
        }
    }
}
