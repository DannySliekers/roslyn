﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Editor.Shared.Utilities;
using Microsoft.CodeAnalysis.ErrorReporting;
using Microsoft.CodeAnalysis.Host.Mef;
using Microsoft.CodeAnalysis.Options;
using Microsoft.Internal.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;

namespace Microsoft.VisualStudio.LanguageServices.Options
{
    [Export(typeof(IOptionPersisterProvider))]
    [Export(typeof(VisualStudioOptionPersisterProvider))]
    internal sealed class VisualStudioOptionPersisterProvider : IOptionPersisterProvider
    {
        private readonly IVsService<SLocalRegistry, ILocalRegistry4> _localRegistryService;
        private readonly IVsService<SVsSettingsPersistenceManager, ISettingsManager> _settingsManagerService;
        private readonly IVsService<SVsFeatureFlags, IVsFeatureFlags> _featureFlagsService;
        private readonly ILegacyGlobalOptionService _legacyGlobalOptions;

        // maps config name to a read fallback:
        private readonly ImmutableDictionary<string, Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> _readFallbacks;

        private VisualStudioOptionPersister? _lazyPersister;

        [ImportingConstructor]
        [Obsolete(MefConstruction.ImportingConstructorMessage, error: true)]
        public VisualStudioOptionPersisterProvider(
            IVsService<SLocalRegistry, ILocalRegistry4> localRegistryService,
            IVsService<SVsSettingsPersistenceManager, ISettingsManager> settingsManagerService,
            IVsService<SVsFeatureFlags, IVsFeatureFlags> featureFlagsService,
            [ImportMany] IEnumerable<Lazy<IVisualStudioStorageReadFallback, OptionNameMetadata>> readFallbacks,
            IThreadingContext threadingContext,
            ILegacyGlobalOptionService legacyGlobalOptions)
        {
            _localRegistryService = localRegistryService;
            _settingsManagerService = settingsManagerService;
            _featureFlagsService = featureFlagsService;
            _legacyGlobalOptions = legacyGlobalOptions;
            _readFallbacks = readFallbacks.ToImmutableDictionary(item => item.Metadata.ConfigName, item => item);
        }

        public async ValueTask<IOptionPersister> GetOrCreatePersisterAsync(CancellationToken cancellationToken)
            => _lazyPersister ??=
                new VisualStudioOptionPersister(
                    new VisualStudioSettingsOptionPersister(RefreshOption, _readFallbacks, await _settingsManagerService.GetValueAsync(cancellationToken).ConfigureAwait(true)),
                    await LocalUserRegistryOptionPersister.CreateAsync(_localRegistryService).ConfigureAwait(false),
                    new FeatureFlagPersister(await TryGetServiceAsync(_featureFlagsService).ConfigureAwait(false)));

        private void RefreshOption(OptionKey2 optionKey, object? newValue)
        {
            if (_legacyGlobalOptions.GlobalOptions.RefreshOption(optionKey, newValue))
            {
                // We may be updating the values of internally defined public options.
                // Update solution snapshots of all workspaces to reflect the new values.
                _legacyGlobalOptions.UpdateRegisteredWorkspaces();
            }
        }

        private static async ValueTask<I?> TryGetServiceAsync<T, I>(IVsService<T, I> service)
            where T : class
            where I : class
        {
            try
            {
                return await service.GetValueAsync().ConfigureAwait(false);
            }
            catch (Exception e) when (FatalError.ReportAndCatch(e))
            {
                return null;
            }
        }
    }
}
