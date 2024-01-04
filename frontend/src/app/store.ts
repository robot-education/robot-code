import { configureStore, ThunkAction, Action } from "@reduxjs/toolkit";
import { onshapeParamsReducer } from "./onshape-params-slice";

export const store = configureStore({
    reducer: {
        onshapeParams: onshapeParamsReducer
    }
});

export type AppDispatch = typeof store.dispatch;
export type RootState = ReturnType<typeof store.getState>;
export type AppThunk<ReturnType = void> = ThunkAction<
    ReturnType,
    RootState,
    unknown,
    Action<string>
>;
