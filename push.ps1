# GitHub

git add .
git stage .
git commit -am update
git push

# DockerHub

docker build . -t fortunen/fine-kube-operator
docker tag fortunen/fine-kube-operator fortunen/fine-kube-operator
docker push fortunen/fine-kube-operator
docker pushrm fortunen/fine-kube-operator # https://github.com/christian-korneck/docker-pushrm
