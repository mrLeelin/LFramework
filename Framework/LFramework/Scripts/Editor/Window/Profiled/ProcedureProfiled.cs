using UnityGameFramework.Runtime;

namespace LFramework.Editor.Window
{
    internal sealed class ProcedureProfiled : ProfiledBase
    {
        internal override bool CanDraw { get; } = true;

        private ProcedureComponent _procedureComponent;

        internal override void Draw()
        {
            GetComponent(ref _procedureComponent);

            bool hasProcedure = _procedureComponent.CurrentProcedure != null;
            DrawMetricCards(
                new ProfiledMetric(
                    "Current Procedure",
                    hasProcedure ? _procedureComponent.CurrentProcedure.GetType().Name : "None",
                    hasProcedure ? _procedureComponent.CurrentProcedure.GetType().FullName : "No active procedure"),
                new ProfiledMetric(
                    "Procedure Time",
                    hasProcedure ? $"{_procedureComponent.CurrentProcedureTime:F1}s" : "N/A",
                    "Elapsed active time"));
        }
    }
}
