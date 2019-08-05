﻿using FlatRedBall.Glue.SaveClasses;
using System;

namespace FlatRedBall.Glue.Plugins.ExportedInterfaces.CommandInterfaces
{
    public interface IGenerateCodeCommands
    {
        void GenerateAllCodeTask();

        [Obsolete("Use GenerateAllCodeTask")]
        void GenerateAllCode();

        void GenerateCurrentElementCode();

        /// <summary>
        /// Generates the current element's code, using
        /// TaskExecutionPreference.AddOrMoveToEnd
        /// </summary>
        void GenerateCurrentElementCodeTask();

        void GenerateElementCode(IElement element);

        void GenerateGlobalContentCode();

        string GetNamespaceForElement(IElement element);

        void GenerateCurrentCsvCode();

        void GenerateAllCodeSync();

        void GenerateCustomClassesCode();

    }
}
