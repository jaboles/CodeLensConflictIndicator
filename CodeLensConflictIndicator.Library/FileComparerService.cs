using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Text.Differencing;
using Microsoft.VisualStudio.Utilities;
using System;
using System.ComponentModel.Composition;

namespace CodeLens.ConflictIndicator
{
    public class FileComparerService
    {
        private static FileComparerService s_instance = new FileComparerService();

        private IServiceProvider serviceProvider;

#pragma warning disable 0649
        [Import]
        ITextDifferencingSelectorService diffSelectorService;
#pragma warning restore 0649

        public static FileComparerService Instance
        {
            get
            {
                return s_instance;
            }
        }

        public void Initialize(IServiceProvider provider)
        {
            this.serviceProvider = provider;
        }

        private void Initialize()
        {
            // satisfied imports
            IComponentModel componentModel = this.serviceProvider.GetService(typeof(SComponentModel)) as IComponentModel;
            if (componentModel == null)
            {
                return;
            }

            componentModel.DefaultCompositionService.SatisfyImportsOnce(this);
        }

        public IHierarchicalDifferenceCollection GetDifferences(IContentType contentType, string left, string right)
        {
            Initialize();

            ITextDifferencingService diffService = this.diffSelectorService.GetTextDifferencingService(contentType);
            StringDifferenceOptions diffOptions = new StringDifferenceOptions(StringDifferenceTypes.Line, 0, true);
            IHierarchicalDifferenceCollection differences = diffService.DiffStrings(left, right, diffOptions);
            return differences;
        }
    }
}
