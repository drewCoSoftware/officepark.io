// NOTE: This was copied from the RelatedSearch project.  We should find a way
// to generalize / share it between apps (NPM?)

// NOTE: This thingy could be used for any of our loading components....
export interface IStatusData {
  HasError: boolean,
  IsLoading: boolean,
  Message: string
}

export interface FetchyOptions {
  method: string | undefined,
  body?: any,
  headers?: {}
}

export interface IApiResponse {
  Code: number;
  Message: string;
}

// This wraps an IApiResponse with a bit of extra data that summarizes success conditions,
// as well as any errors that may have been encountered while processing the request.
// NOTE: 'Error' data member does not pertain to things like 404, 500, etc. status codes.
export interface FetchyResponse<T extends IApiResponse> {
  Data: T | null;
  Success: boolean;
  Error: any | null;
  StatusCode: number;

  // TODO: We can care about headers, etc. later??
}

// ----------------------------------------------------------------------------------------------------------
export async function fetchyPost<T extends IApiResponse>(url: string, data: any | null, headers: {} | undefined = undefined): Promise<FetchyResponse<T>> {

  let p = fetchy<T>(url, {
    method: 'POST',
    body: data == null ? null : JSON.stringify(data),
    headers: headers
  });

  return p;
}

// ----------------------------------------------------------------------------------------------------------
export async function fetchy<T extends IApiResponse>(url: string, ops: FetchyOptions | null = null): Promise<FetchyResponse<T>> {
  // Populate default as needed.....
  if (ops == null) {
    ops = {
      method: 'GET'
    }
  }

  let res: FetchyResponse<T> = {
    Success: false,
    Data: null,
    Error: null,
    StatusCode: 0
  }

  let p = fetch(url, {
    method: ops.method,
    body: ops.body,
    headers: ops.headers,
    credentials: "include"    // TODO: FETCHY needs to be some kind of configurable functor.
  })

  let success = true;
  let statusCode = 0;
  await p.then(response => {
    statusCode = response.status;
    success = statusCode == 200;   // OPTIONS: We could configure to pass/not other status codes.
    const res = response.json();
    return res;
  }).then(data => {

    // NOTE: This will not deserialize the date strings into proper
    // Date instances for typescript.
    // We may have to look at our intended property types from <T>
    // and find ways to convert from there.  Definitiely NOT something that
    // we want to mess with at this time.
    res.Data = <T>data;
    res.Success = success;
    res.StatusCode = statusCode;

  }).catch((error) => {

    // Errors happen when there is some kind of network issue.
    res.Success = false;
    res.Error = error
  });

  return res;
}

