/*
 * Copyright 2021 Palantir Technologies, Inc. All rights reserved.
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

import * as React from "react";
import type { SVGIconProps } from "../../svgIconProps";
import { IconSize } from "../../iconTypes";
import { SVGIconContainer } from "../../svgIconContainer";

export const MapMarker: React.FC<SVGIconProps> = React.forwardRef<any, SVGIconProps>((props, ref) => {
    const isLarge = props.size! >= IconSize.LARGE;
    const pixelGridSize = isLarge ? IconSize.LARGE : IconSize.STANDARD;
    const translation = `${-1 * pixelGridSize / 0.05 / 2}`;
    return (
        <SVGIconContainer iconName="map-marker" ref={ref}  {...props}>
            <path
                d={isLarge ? "M199.6 400C122.2 400 59.6 340.4 59.6 266.6C59.6 193 199.6 0 199.6 0S339.6 193 339.6 266.6C339.6 340.2 276.8 400 199.6 400zM199.6 200C166.4 200 139.6 226.8 139.6 260S166.4 320 199.6 320S259.6 293.2 259.6 260S232.8 200 199.6 200z" : "M169.2 320C108.4 320 59 272.2 59 213.4C59 154.4 169.2 0 169.2 0S279.4000000000001 154.4 279.4000000000001 213.4C279.2000000000001 272.2 230 320 169.2 320zM169.2 160C141.6 160 119.2 182.4 119.2 210C119.2 237.6 141.6 260 169.2 260C196.8 260 219.2 237.6 219.2 210C219.2 182.4 196.8 160 169.2 160z"}
                fillRule="evenodd"
                transform-origin="center"
                transform={`scale(0.05, -0.05) translate(${translation}, ${translation})`}
            />
        </SVGIconContainer>
   )
});
MapMarker.defaultProps = {
    size: IconSize.STANDARD,
};
MapMarker.displayName = `Blueprint5.Icon.MapMarker`;
export default MapMarker;
