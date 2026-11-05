using NUnit.Framework;
using UnityEngine;

namespace CoffinEscape.Tests
{
    public sealed class ReturnToAnchorTests
    {
        private GameObject itemObject;
        private GameObject anchorObject;

        [SetUp]
        public void SetUp()
        {
            itemObject = new GameObject("ReturnToAnchorTestItem");
            itemObject.AddComponent<Rigidbody>();
            itemObject.AddComponent<BoxCollider>();
            ReturnToAnchor returnToAnchor = itemObject.AddComponent<ReturnToAnchor>();

            anchorObject = new GameObject("ReturnToAnchorTestAnchor");
            anchorObject.transform.SetPositionAndRotation(new Vector3(1.25f, 0.3f, 0.8f), Quaternion.Euler(0f, 35f, 0f));
            returnToAnchor.Configure(anchorObject.transform, null);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(itemObject);
            Object.DestroyImmediate(anchorObject);
        }

        [Test]
        public void ReturnHomeUsesConfiguredAnchorPose()
        {
            itemObject.transform.SetPositionAndRotation(new Vector3(-2f, 4f, -1f), Quaternion.identity);
            Rigidbody body = itemObject.GetComponent<Rigidbody>();

            itemObject.GetComponent<ReturnToAnchor>().ReturnHome();

            Assert.That(Vector3.Distance(itemObject.transform.position, anchorObject.transform.position), Is.LessThan(0.001f));
            Assert.That(Quaternion.Angle(itemObject.transform.rotation, anchorObject.transform.rotation), Is.LessThan(0.001f));
            Assert.That(Vector3.Distance(body.position, anchorObject.transform.position), Is.LessThan(0.001f));
        }
    }
}
