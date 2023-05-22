from library import *
from library.core.feature import FeatureFactory

ROBOT_DESCRIPTION = r"""

Documentation may be found at: 
<b>robothandbook.dev</b>

FeatureScript by Alex Kempen."""


class RobotFeature:
    def __init__(self, name: str, add_robot: bool = True) -> None:
        if add_robot:
            self.name = "robot" + name.capitalize()
        else:
            self.name = name

    def make_feature_studio(self, description: str) -> PartialStudio:
        studio = PartialStudio(self.name + ".fs", "backend").add(
            FeatureFactory()
            .start(
                self.name,
                description=description + ROBOT_DESCRIPTION,
                filter_selector=["ROBOT"],
            )
            .make()
        )
        return studio

    def make_ui_studio(self, feature_studio: Studio | None = None) -> Studio:
        ui_name = self.name + "Ui.gen.fs"
        ui_studio = Studio(ui_name, "backend")
        if feature_studio:
            feature_studio.add_import(ui_name, "backend", True)
        return ui_studio
