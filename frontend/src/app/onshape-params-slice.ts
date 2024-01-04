import { createSelector, createSlice } from "@reduxjs/toolkit";
import { RootState } from "./store";
import { AppType } from "../common/app-type";
import { ElementPath, getCurrentPath, toDocumentPath } from "../api/path";

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

export const selectDocumentPath = createSelector(
    selectOnshapeParams,
    (params) => toDocumentPath(params)
);

export const selectElementPath = (state: RootState) =>
    state.onshapeParams as ElementPath;

export const onshapeParamsReducer = onshapeParamsSlice.reducer;
