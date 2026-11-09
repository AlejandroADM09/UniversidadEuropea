using UnityEditor;
using UnityEngine.UIElements;

namespace Unity.Entities.Editor
{
    class QueryWithEntitiesView : FoldoutWithoutActionButton
    {
        readonly QueryWithEntitiesViewData m_Data;
        readonly VisualElement m_EntitiesContainer;
        static readonly string k_Query = L10n.Tr("Query");

        public QueryWithEntitiesView(in QueryWithEntitiesViewData data)
        {
            m_Data = data;
            Resources.Templates.QueryWithEntities.AddStyles(this);
            this.Q(className: "unity-foldout__content").AddToClassList(UssClasses.QueryWithEntities.ToggleContent);

            HeaderName.text = $"{k_Query} #{data.QueryOrder}";
            MatchingCount.text = "0";

            m_EntitiesContainer = new VisualElement();
            Add(m_EntitiesContainer);

            SetValueWithoutNotify(true);
        }

        public void Update()
        {
            if (!m_Data.Update())
                return;

            MatchingCount.text = m_Data.TotalEntityCount.ToString();
            m_EntitiesContainer.Clear();
            foreach (var entity in m_Data.Entities)
            {
                m_EntitiesContainer.Add(new EntityView(entity));
            }
        }
    }
}
