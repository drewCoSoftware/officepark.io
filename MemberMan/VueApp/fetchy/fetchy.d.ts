export interface IStatusData {
    HasError: boolean;
    IsLoading: boolean;
    Message: string;
}
export interface FetchyOptions {
    method: string | undefined;
    body?: any;
    headers?: {};
}
export interface IApiResponse {
    Code: number;
    Message: string;
}
export interface FetchyResponse<T extends IApiResponse> {
    Data: T | null;
    Success: boolean;
    Error: any | null;
    StatusCode: number;
}
export declare function fetchyPost<T extends IApiResponse>(url: string, data: any | null, headers?: {} | undefined): Promise<FetchyResponse<T>>;
export declare function fetchy<T extends IApiResponse>(url: string, ops?: FetchyOptions | null): Promise<FetchyResponse<T>>;
