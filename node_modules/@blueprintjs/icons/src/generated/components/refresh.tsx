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

export const Refresh: React.FC<SVGIconProps> = React.forwardRef<any, SVGIconProps>((props, ref) => {
    const isLarge = props.size! >= IconSize.LARGE;
    const pixelGridSize = isLarge ? IconSize.LARGE : IconSize.STANDARD;
    const translation = `${-1 * pixelGridSize / 0.05 / 2}`;
    return (
        <SVGIconContainer iconName="refresh" ref={ref}  {...props}>
            <path
                d={isLarge ? "M380 380C369 380 360 371 360 360V318.8C323.6 367.8 265.8 400 200 400C89.6 400 0 310.4 0 200C0 189 9 180 20 180S40 189 40 200C40 288.4 111.6 360 200 360C250.4 360 295.2 336.4 324.4 300H300C289 300 280 291 280 280C280 269 289 260 300 260H380C391 260 400 269 400 280V360C400 371 391 380 380 380zM380 220C369 220 360 211 360 200C360 111.6 288.4 40 200 40C149.6 40 104.8 63.6 75.6 100H100C111 100 120 109 120 120C120 131 111 140 100 140H20C9 140 0 131 0 120V40C0 29 9 20 20 20S40 29 40 40V81.2C76.4 32.2 134.2 0 200 0C310.4 0 400 89.6 400 200C400 211 391 220 380 220z" : "M299.8 180.2C288.8 180.2 279.8 171.2 279.8 160.2C279.8 94 226 40.2 159.8 40.2C124.4 40.2 92.6 55.8 70.6 80.2H99.8C110.8 80.2 119.8 89.2 119.8 100.2S110.8 120.2 99.8 120.2H19.8C8.8 120.2 -0.2 111.2 -0.2 100.2V20.2C-0.2 9.2 8.8 0.2 19.8 0.2S39.8 9.2 39.8 20.2V55C69 21.6 111.8 0.2 159.8 0.2C248.2 0.2 319.8 71.8 319.8 160.2C319.8 171.2 310.8 180.2 299.8 180.2zM299.8 320.2C288.8 320.2 279.8 311.2 279.8 300.2V265.4C250.6 298.8 207.8 320.2 159.8 320.2C71.4 320.2 -0.2 248.6 -0.2 160.2C-0.2 149.2 8.8 140.2 19.8 140.2S39.8 149.2 39.8 160.2C39.8 226.4 93.6 280.2 159.8 280.2C195.2 280.2 227 264.6 249 240.2H219.8C208.8 240.2 199.8 231.2 199.8 220.2S208.8 200.2 219.8 200.2H299.8C310.8 200.2 319.8 209.2 319.8 220.2V300.2C319.8 311.2 310.8 320.2 299.8 320.2z"}
                fillRule="evenodd"
                transform-origin="center"
                transform={`scale(0.05, -0.05) translate(${translation}, ${translation})`}
            />
        </SVGIconContainer>
   )
});
Refresh.defaultProps = {
    size: IconSize.STANDARD,
};
Refresh.displayName = `Blueprint5.Icon.Refresh`;
export default Refresh;
