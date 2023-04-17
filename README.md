CRDS
====
I'm taking the helm 'approach'
https://helm.sh/docs/topics/charts/#limitations-on-crds

Unlike most objects in Kubernetes, CRDs are installed globally. For that reason, Helm takes a very cautious approach in managing CRDs. CRDs are subject to the following limitations:

- CRDs are never reinstalled. If Helm determines that the CRDs in the crds/ directory are already present (regardless of version), Helm will not attempt to install or upgrade.
- CRDs are never installed on upgrade or rollback. Helm will only create CRDs on installation operations.
-CRDs are never deleted. Deleting a CRD automatically deletes all of the CRD's contents across all namespaces in the cluster. Consequently, Helm will not delete CRDs

Operators who want to upgrade or delete CRDs are encouraged to do this manually and with great care.