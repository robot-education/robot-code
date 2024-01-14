import { createSlice } from "@reduxjs/toolkit";
import type { RootState } from "./store";
import { AppType } from "../common/app-type";
import {
    ElementPath,
    getCurrentPath,
    toApiDocumentPath,
    toApiElementPath
} from "../api/path";

export interface OnshapeParams extends ElementPath {
    appType: AppType;
}

function getInitialState(): OnshapeParams {
    const params = new URLSearchParams(window.location.search);
    return {
        ...getCurrentPath(params),
        appType: params.get("appType") as AppType
    };
}

export const onshapeParamsSlice = createSlice({
    name: "onshapeParams",
    initialState: getInitialState,
    reducers: {}
});

export const selectOnshapeParams = (state: RootState) => state.onshapeParams;

export const selectAppType = (state: RootState) => state.onshapeParams.appType;

export const selectApiDocumentPath = (state: RootState) =>
    toApiDocumentPath(state.onshapeParams);

export const selectApiElementPath = (state: RootState) =>
    toApiElementPath(state.onshapeParams);

export const onshapeParamsReducer = onshapeParamsSlice.reducer;
