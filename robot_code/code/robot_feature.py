from featurescript import *
from featurescript.core.feature import FeatureBuilder

ROBOT_DESCRIPTION: str = r"""

Documentation may be found at: 
<b>robothandbook.dev</b>

FeatureScript by Alex Kempen."""


class RobotFeature:
    def __init__(
        self, name: str, add_robot: bool = True, description: str | None = None
    ) -> None:
        if add_robot:
            self.name = "robot" + name.capitalize()
        else:
            self.name = name

        # self.feature_studio = self.make_feature_studio(description or "")
        self.ui_studio = self.make_ui_studio()
        # self.feature_studio.add_import(self.ui_studio.studio_name, export=True)

    # def make_feature_studio(self, description: str) -> PartialStudio:
    #     studio = PartialStudio(self.name + ".fs").add(
    #         FeatureBuilder()
    #         .start(
    #             self.name,
    #             description=description + ROBOT_DESCRIPTION,
    #         )
    #         .build()
    #     )
    #     return studio

    def make_ui_studio(self) -> Studio:
        ui_name = self.name + "Ui.gen.fs"
        return Studio(ui_name)
