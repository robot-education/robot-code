from library.api import api_base, api_call, conf, api_path


def get_feature(
    api: api_base.Api, part_studio_path: api_path.ElementPath, feature_id: str
) -> object:
    return api_base.get(
        api, api_path.api_path("partstudios", part_studio_path, "features")
    )


def main():
    api = api_base.Api(logging=False)
    config = conf.Config()
    backend = config.get_document("backend")
    if backend == None:
        raise ValueError("Failed to find backend?")
    document = api_call.get_document_elements(api, backend)


if __name__ == "__main__":
    main()
